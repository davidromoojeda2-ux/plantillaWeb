namespace paginaWeb.Areas.Admin.Models;

/// <summary>
/// Datos enviados desde el editor para validar un artículo antes de guardarlo.
/// </summary>
public sealed record ArticleValidationRequest(
    string? Title,
    string? Summary,
    string? Slug,
    string? Author,
    string? CategoryId,
    string? CoverImageUrl,
    string? Status,
    string? BlocksJson);
