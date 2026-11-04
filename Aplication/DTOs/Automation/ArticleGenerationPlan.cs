using Domine.Entities;

namespace Aplication.DTOs.Automation;

/// <summary>
/// Parámetros efectivos de generación: combina la configuración guardada con los
/// estándares automáticos derivados de la longitud del artículo.
/// </summary>
public sealed record ArticleGenerationPlan(
    int TargetWords,
    int MaxTokens,
    int MinBlocks,
    int WordTolerancePercent,
    int MaxGenerationAttempts,
    string Template,
    string Tone,
    double Temperature,
    bool IncludeList,
    bool IncludeTable,
    bool IncludeQuote,
    bool IncludeConclusion,
    bool IncludeFaq,
    bool IncludeCta,
    bool IncludeLinks);

/// <summary>Construye el plan efectivo a partir de la configuración de automatización.</summary>
public static class ArticleGenerationPlanner
{
    public static ArticleGenerationPlan FromSettings(AutomationSettings settings)
    {
        var standard = settings.AutoStandards
            ? ArticleGenerationStandards.ForWords(settings.TargetWords)
            : null;

        return new ArticleGenerationPlan(
            TargetWords: settings.TargetWords,
            MaxTokens: standard?.MaxTokens
                ?? (settings.MaxTokensPerArticle > 0
                    ? settings.MaxTokensPerArticle
                    : ArticleLengthOptions.EstimateMaxTokens(settings.TargetWords)),
            MinBlocks: standard?.MinBlocks ?? settings.MinBlocks,
            WordTolerancePercent: standard?.WordTolerancePercent ?? settings.WordTolerancePercent,
            MaxGenerationAttempts: Math.Max(1, settings.MaxGenerationAttempts),
            Template: settings.ArticleTemplate,
            Tone: settings.Tone,
            Temperature: settings.Temperature,
            IncludeList: settings.IncludeList,
            IncludeTable: settings.IncludeTable,
            IncludeQuote: settings.IncludeQuote,
            IncludeConclusion: settings.IncludeConclusion,
            IncludeFaq: settings.IncludeFaq,
            IncludeCta: settings.IncludeCta,
            IncludeLinks: settings.IncludeLinks);
    }
}
