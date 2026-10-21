using Aplication.DTOs;

namespace paginaWeb.Models.Category;

/// <summary>
/// Representa los datos requeridos para renderizar una tarjeta de artículo estándar
/// en el listado de categoría.
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
/// ViewModel fuertemente tipado para la vista de listado por categoría.
/// Soporta tanto categorías principales como subcategorías, proveyendo acceso
/// a la categoría padre, subcategoría actual y lista de subcategorías asociadas.
/// </summary>
public sealed record CategoryPageViewModel(
    CategoryResponseDto Category,
    int CurrentPage,
    int TotalPages,
    long TotalCount,
    IReadOnlyList<ArticleCardViewModel> Articles,
    CategoryResponseDto? ParentCategory = null,
    IReadOnlyList<CategoryResponseDto>? Subcategories = null,
    CategoryResponseDto? Subcategory = null)
{
    /// <summary>
    /// Retorna la subcategoría si la categoría actual es una subcategoría o si Subcategory fue asignada.
    /// </summary>
    public CategoryResponseDto? Subcategoria => Subcategory ?? (ParentCategory != null ? Category : null);

    /// <summary>Alias en español para la categoría padre.</summary>
    public CategoryResponseDto? CategoriaPadre => ParentCategory;

    /// <summary>Indica si la vista actual corresponde a una subcategoría.</summary>
    public bool IsSubcategory => ParentCategory != null || !string.IsNullOrEmpty(Category.ParentId);

    /// <summary>Indica si existen subcategorías asociadas para mostrar.</summary>
    public bool HasSubcategories => Subcategories is { Count: > 0 };
}
