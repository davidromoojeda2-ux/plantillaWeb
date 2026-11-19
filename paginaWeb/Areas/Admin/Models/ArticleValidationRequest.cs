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

/// <summary>Observación enviada al corrector de IA.</summary>
public sealed record ArticleCorrectionObservation(string? Code, string? Message);

/// <summary>
/// Datos del editor para pedir una corrección con IA. Las observaciones son los
/// mensajes que el usuario ve en el panel de validación.
/// </summary>
public sealed record ArticleCorrectionInput(
    string? Title,
    string? Summary,
    string? BlocksJson,
    IReadOnlyList<ArticleCorrectionObservation>? Issues);
