namespace Aplication.DTOs;

/// <summary>Severidades usadas por los hallazgos de validación.</summary>
public static class ArticleIssueSeverity
{
    public const string Error = "error";
    public const string Warning = "warning";
}

/// <summary>
/// Hallazgo accionable de la validación. Incluye el campo o los bloques a los que
/// apunta para que el editor pueda llevar al usuario al punto observado.
/// </summary>
public sealed record ArticleValidationIssue(
    string Severity,
    string Code,
    string Message,
    string? Field = null,
    IReadOnlyList<int>? BlockIndexes = null,
    string? Url = null);

/// <summary>
/// Resultado de la validación editorial de un artículo antes de guardarlo o publicarlo.
/// </summary>
public sealed record ArticleFormValidationResult(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Links,
    IReadOnlyList<ArticleValidationIssue>? Issues = null)
{
    public IReadOnlyList<ArticleValidationIssue> EffectiveIssues => Issues ?? [];
}

/// <summary>
/// Estado de un enlace comprobado por HTTP.
/// </summary>
public sealed record ArticleLinkCheckResult(
    string Url,
    int? StatusCode,
    string Status,
    string? Detail)
{
    public bool IsBroken => Status is "caido" or "sin-respuesta" or "error-servidor";

    public bool IsWarning => Status == "restringido";
}
