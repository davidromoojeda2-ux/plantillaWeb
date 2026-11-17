namespace Aplication.DTOs;

/// <summary>
/// Resultado de la validación editorial de un artículo antes de guardarlo o publicarlo.
/// </summary>
public sealed record ArticleFormValidationResult(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Links);

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
