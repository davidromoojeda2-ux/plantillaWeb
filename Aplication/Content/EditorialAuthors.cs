namespace Aplication.Content;

/// <summary>
/// Perfil editorial con el que se firma un artículo: nombre público, cargo,
/// biografía y datos opcionales de presentación.
/// </summary>
public sealed record EditorialAuthorProfile(
    string Name,
    string Role,
    string Bio,
    string? AvatarUrl = null,
    string? SocialUrl = null);

/// <summary>
/// Firmas editoriales del portal. Kely Pantoja firma las secciones de guías por
/// comunidad, ayudas y subvenciones, ahorro y comunidades de vecinos; el resto
/// de secciones se atribuyen al Grupo de Redactores de unautonomo.
/// </summary>
public static class EditorialAuthors
{
    public static readonly EditorialAuthorProfile KelyPantoja = new(
        "Kely Pantoja",
        "Redactora",
        "Periodista especializada en energía y autoconsumo. Analiza ayudas, subvenciones y trámites para que cada instalación sea rentable, y explica con ejemplos reales cómo ahorrar en la factura o poner de acuerdo a una comunidad de vecinos.");

    public static readonly EditorialAuthorProfile EditorialGroup = new(
        "Grupo de Redactores de unautonomo",
        "Equipo editorial",
        "Equipo de redacción de unautonomo dedicado a la divulgación técnica solar. Contrastamos equipos, precios y experiencias de instalación para ofrecer comparativas y guías prácticas sobre placas, baterías y mantenimiento.");

    /// <summary>Slugs de las secciones cuya autoría corresponde a Kely Pantoja.</summary>
    private static readonly HashSet<string> KelyCategorySlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "guias-por-comunidad-autonoma",
        "ayudas-y-subvenciones",
        "comunidades-de-vecinos",
        "ahorro-y-rentabilidad"
    };

    /// <summary>Nombres equivalentes, por si el slug de la categoría cambia.</summary>
    private static readonly HashSet<string> KelyCategoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "guias por comunidad autonoma",
        "ayudas y subvenciones",
        "comunidades de vecinos",
        "ahorro y rentabilidad"
    };

    public static bool SignsAsKely(string? categorySlug, string? categoryName = null) =>
        (!string.IsNullOrWhiteSpace(categorySlug) && KelyCategorySlugs.Contains(categorySlug.Trim())) ||
        (!string.IsNullOrWhiteSpace(categoryName) && KelyCategoryNames.Contains(Normalize(categoryName)));

    public static EditorialAuthorProfile Resolve(string? categorySlug, string? categoryName = null) =>
        SignsAsKely(categorySlug, categoryName) ? KelyPantoja : EditorialGroup;

    private static string Normalize(string value) =>
        value.Trim()
            .ToLowerInvariant()
            .Replace('á', 'a')
            .Replace('é', 'e')
            .Replace('í', 'i')
            .Replace('ó', 'o')
            .Replace('ú', 'u')
            .Replace('ü', 'u');
}
