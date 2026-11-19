using Aplication.DTOs.Automation;

namespace Aplication.Services;

/// <summary>Resultado de preparar un borrador: perfil aplicado y hallazgos pendientes.</summary>
public sealed record ArticleDraftStyle(
    ParagraphStyleProfile Profile,
    IReadOnlyList<ParagraphFinding> Findings)
{
    public bool HasHardIssues => Findings.Any(f => f.Severity == ParagraphSeverity.Hard);
}

/// <summary>
/// Orquestación compartida por todos los flujos: filtra secciones no solicitadas,
/// normaliza párrafos y analiza el resultado. El orden se define una sola vez.
/// </summary>
public static class ArticleDraftPipeline
{
    public static ArticleDraftStyle Process(GeneratedArticle article, ArticleGenerationPlan plan)
    {
        GeneratedArticleSectionFilter.Apply(article, plan);

        var profile = ParagraphStyleProfile.Resolve(plan.Template, plan.ParagraphStyles, plan.Language);
        ParagraphNormalizer.Apply(article.Blocks, profile);

        var findings = ParagraphStyle.Analyze(article.Blocks, profile);
        return new ArticleDraftStyle(profile, findings);
    }
}
