using Aplication.DTOs;
using paginaWeb.Models.Category;

namespace paginaWeb.Models.Publicaciones;

/// <summary>
/// ViewModel fuertemente tipado para la vista de publicaciones y catálogo de artículos.
/// </summary>
public sealed record PublicacionesPageViewModel(
    string Query,
    int CurrentPage,
    int TotalPages,
    long TotalCount,
    IReadOnlyList<ArticleCardViewModel> Items)
{
    /// <summary>Indica si la vista actual corresponde a una búsqueda con filtro de texto.</summary>
    public bool IsSearch => !string.IsNullOrWhiteSpace(Query);
}

/// <summary>
/// Modelo y opciones de configuración para el componente parcial modular de búsqueda (_SearchBar).
/// Permite inyectar la barra de búsqueda en cualquier vista con soporte de estado activo y contador.
/// </summary>
public sealed record SearchBarViewModel(
    string? Query = null,
    long? TotalCount = null,
    bool ShowCount = false,
    string Placeholder = "Buscar noticias, temas o autores…",
    string Action = "Index",
    string Controller = "Publicaciones",
    string? FormClass = null,
    string? InputClass = null,
    string? ButtonClass = null,
    string? InputId = null,
    bool IsHeroSearch = false)
{
    /// <summary>Indica si hay un término de búsqueda activo.</summary>
    public bool IsSearch => !string.IsNullOrWhiteSpace(Query);

    public static implicit operator SearchBarViewModel(string? query) => new(Query: query);
}
