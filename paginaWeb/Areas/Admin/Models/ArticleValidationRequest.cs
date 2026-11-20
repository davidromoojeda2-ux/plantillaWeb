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

/// <summary>
/// Enlaces caídos que el usuario confirmó quitar del cuerpo. El servidor vuelve a
/// comprobarlos antes de eliminar sus etiquetas para no borrar enlaces ya válidos.
/// </summary>
public sealed record ArticleLinkRemovalInput(
    string? BlocksJson,
    IReadOnlyList<string>? Urls,
    bool Confirmed = false);

/// <summary>Acción de enlace confirmada por el usuario.</summary>
public sealed record ArticleLinkActionInput(
    string? Type,
    string? Url,
    string? Replacement);

/// <summary>
/// Cambios de enlaces que el usuario confirmó. El servidor vuelve a validar las URL
/// antes de sustituir o quitar las etiquetas.
/// </summary>
public sealed record ArticleLinkActionsApplyInput(
    string? BlocksJson,
    IReadOnlyList<ArticleLinkActionInput>? Actions,
    bool Confirmed = false);
