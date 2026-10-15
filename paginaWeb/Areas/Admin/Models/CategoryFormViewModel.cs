using System.ComponentModel.DataAnnotations;
using Aplication.DTOs;

namespace paginaWeb.Areas.Admin.Models;

/// <summary>
/// Modelo de presentación (ViewModel) para la creación y edición de categorías en el panel administrativo.
/// Agrupa los datos del formulario junto a la lista fuertemente tipada de categorías padre.
/// </summary>
public class CategoryFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    [StringLength(100, ErrorMessage = "El slug no puede superar los 100 caracteres.")]
    public string Slug { get; set; } = string.Empty;

    public string? ParentId { get; set; }

    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? Description { get; set; }

    public int Order { get; set; } = 0;

    public bool ShowInMenu { get; set; } = true;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Lista de categorías disponibles para actuar como padre en la jerarquía.
    /// </summary>
    public List<CategoryResponseDto> Parents { get; set; } = [];

    public CategoryDto ToDto() => new()
    {
        Name = Name,
        Slug = Slug,
        ParentId = ParentId,
        Description = Description,
        Order = Order,
        ShowInMenu = ShowInMenu,
        IsActive = IsActive
    };

    public static CategoryFormViewModel FromDto(CategoryResponseDto dto, List<CategoryResponseDto> parents) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Slug = dto.Slug,
        ParentId = dto.ParentId,
        Description = dto.Description,
        Order = dto.Order,
        ShowInMenu = dto.ShowInMenu,
        IsActive = dto.IsActive,
        Parents = parents
    };
}
