using Aplication.DTOs;

namespace paginaWeb.Models;

/// <summary>
/// Representa los datos requeridos para renderizar una tarjeta de artículo estándar.
/// </summary>
public sealed record ArticleCardViewModel(
    ArticleResponseDto Article,
    string? ImageUrl,
    string CategoryName)
{
    public static implicit operator (ArticleResponseDto Article, string? ImageUrl, string? CategoryName)(ArticleCardViewModel vm)
        => (vm.Article, vm.ImageUrl, vm.CategoryName);
}

/// <summary>
/// ViewModel fuertemente tipado para los resultados y estado de la búsqueda.
/// </summary>
public sealed record SearchViewModel(
    string Query,
    int CurrentPage,
    int TotalPages,
    long TotalCount,
    IReadOnlyList<ArticleCardViewModel> Items);

/// <summary>
/// ViewModel fuertemente tipado para la vista de listado por categoría.
/// </summary>
public sealed record CategoryPageViewModel(
    CategoryResponseDto Category,
    int CurrentPage,
    int TotalPages,
    long TotalCount,
    IReadOnlyList<ArticleCardViewModel> Articles);
