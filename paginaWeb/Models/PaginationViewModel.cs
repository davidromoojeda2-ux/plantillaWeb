namespace paginaWeb.Models;

/// <summary>
/// Modelo para el componente modular de paginación reutilizable.
/// Encapsula la lógica de navegación, cálculo de ventanas (sliding window), elipsis y parámetros de ruta.
/// </summary>
public class PaginationViewModel
{
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public string Action { get; set; } = "Index";
    public string Controller { get; set; } = "Publicaciones";
    public string PageRouteKey { get; set; } = "page";
    public string AriaLabel { get; set; } = "Navegación de páginas";
    public Dictionary<string, string> RouteValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool ShouldRender => TotalPages > 1;
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
    public int PreviousPage => Math.Max(1, CurrentPage - 1);
    public int NextPage => Math.Min(TotalPages, CurrentPage + 1);

    public PaginationViewModel() { }

    public PaginationViewModel(
        int currentPage,
        int totalPages,
        string action,
        string controller,
        IDictionary<string, string?>? routeValues = null,
        string pageRouteKey = "page",
        string ariaLabel = "Navegación de páginas")
    {
        CurrentPage = currentPage < 1 ? 1 : currentPage;
        TotalPages = totalPages < 1 ? 1 : totalPages;
        Action = action;
        Controller = controller;
        PageRouteKey = pageRouteKey;
        AriaLabel = ariaLabel;

        if (routeValues != null)
        {
            foreach (var kvp in routeValues)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                {
                    RouteValues[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    /// <summary>
    /// Genera el diccionario de parámetros de ruta para una página específica.
    /// </summary>
    public Dictionary<string, string> GetRouteData(int page)
    {
        var dict = new Dictionary<string, string>(RouteValues, StringComparer.OrdinalIgnoreCase)
        {
            [PageRouteKey] = page.ToString()
        };
        return dict;
    }

    /// <summary>
    /// Genera la secuencia inteligente de páginas a mostrar con elipsis (...) para listas largas.
    /// </summary>
    public IEnumerable<PaginationItem> GetPages()
    {
        if (TotalPages <= 7)
        {
            for (int i = 1; i <= TotalPages; i++)
            {
                yield return new PaginationItem(i, i == CurrentPage);
            }
            yield break;
        }

        // Siempre la primera página
        yield return new PaginationItem(1, CurrentPage == 1);

        int start = Math.Max(2, CurrentPage - 1);
        int end = Math.Min(TotalPages - 1, CurrentPage + 1);

        // Elipsis antes de la ventana si es necesario
        if (start > 2)
        {
            yield return PaginationItem.Ellipsis;
        }

        // Ventana central alrededor de la página actual
        for (int i = start; i <= end; i++)
        {
            yield return new PaginationItem(i, i == CurrentPage);
        }

        // Elipsis después de la ventana si es necesario
        if (end < TotalPages - 1)
        {
            yield return PaginationItem.Ellipsis;
        }

        // Siempre la última página
        yield return new PaginationItem(TotalPages, CurrentPage == TotalPages);
    }

    /// <summary>
    /// Fábrica para la vista de publicaciones con soporte para búsqueda.
    /// </summary>
    public static PaginationViewModel ForPublicaciones(int currentPage, int totalPages, string? query = null)
    {
        var routes = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(query))
        {
            routes["q"] = query;
        }
        return new PaginationViewModel(currentPage, totalPages, "Index", "Publicaciones", routes);
    }

    /// <summary>
    /// Fábrica para la vista de categorías con soporte para el slug.
    /// </summary>
    public static PaginationViewModel ForCategory(int currentPage, int totalPages, string slug)
    {
        var routes = new Dictionary<string, string?>
        {
            ["slug"] = slug
        };
        return new PaginationViewModel(currentPage, totalPages, "Categoria", "Category", routes);
    }
}

/// <summary>
/// Elemento individual de página (número o elipsis).
/// </summary>
public readonly record struct PaginationItem(int? PageNumber, bool IsCurrent)
{
    public bool IsEllipsis => !PageNumber.HasValue;
    public static PaginationItem Ellipsis => new(null, false);
}
