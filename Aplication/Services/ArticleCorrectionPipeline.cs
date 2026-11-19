using Aplication.DTOs.Automation;
using Aplication.Interfaces;

namespace Aplication.Services;

/// <summary>Resultado de corregir con IA y verificar el estilo con las reglas del panel.</summary>
public sealed record ArticleCorrectionOutcome(
    GeneratedArticle Article,
    IReadOnlyList<ParagraphFinding> RemainingFindings,
    int Attempts);

/// <summary>
/// Orquesta la corrección con IA: reescribe, normaliza los párrafos y vuelve a
/// analizarlos con las mismas reglas que usa el panel. Si quedan hallazgos, reintenta
/// con instrucciones concretas por bloque en lugar de mensajes agregados.
/// </summary>
public static class ArticleCorrectionPipeline
{
    /// <summary>
    /// Mínimo dos intentos (uno de corrección y otro de verificación) y como máximo
    /// tres, siguiendo la configuración de reintentos de la automatización.
    /// </summary>
    public const int MinAttempts = 2;
    public const int MaxAttempts = 3;

    public static async Task<ArticleCorrectionOutcome> CorrectAsync(
        GeneratedArticle source,
        IReadOnlyList<string> issues,
        ArticleGenerationPlan plan,
        ArticleCorrectionRequest config,
        IAiArticleCorrector corrector,
        CancellationToken ct = default)
    {
        var maxAttempts = Math.Clamp(plan.MaxGenerationAttempts, MinAttempts, MaxAttempts);
        var profile = ParagraphStyleProfile.Resolve(
            plan.Template, plan.ParagraphStyles, plan.Language);

        var externalIssues = issues
            .Where(issue => !string.IsNullOrWhiteSpace(issue))
            .Select(issue => issue.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var current = source;
        var instructions = externalIssues
            .Concat(ParagraphStyle.DescribeFindingsForPrompt(
                ParagraphStyle.Analyze(source.Blocks, profile), source.Blocks, profile))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        for (var attempt = 1; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var corrected = await corrector.CorrectAsync(new ArticleCorrectionRequest
            {
                Title = current.Title,
                Summary = current.Summary,
                Blocks = current.Blocks,
                Language = plan.Language,
                Template = plan.Template,
                ParagraphStyles = plan.ParagraphStyles,
                Issues = instructions,
                InternalLinks = config.InternalLinks,
                Temperature = config.Temperature,
                MaxTokens = config.MaxTokens,
                ApiKey = config.ApiKey,
                BaseUrl = config.BaseUrl,
                Model = config.Model
            }, ct);

            if (corrected.Blocks.Count == 0)
                throw new InvalidOperationException(
                    "La IA no devolvió bloques para el artículo. Inténtalo de nuevo.");

            // Misma normalización que la automatización y, además, fusión de párrafos
            // demasiado cortos: la corrección manual debe resolver los avisos, no solo
            // señalarlos.
            ParagraphNormalizer.Apply(corrected.Blocks, profile, mergeShort: true);

            var findings = ParagraphStyle.Analyze(corrected.Blocks, profile);
            if (findings.Count == 0 || attempt >= maxAttempts)
                return new ArticleCorrectionOutcome(corrected, findings, attempt);

            current = corrected;
            instructions = ParagraphStyle
                .DescribeFindingsForPrompt(findings, corrected.Blocks, profile)
                .ToList();
        }
    }
}
