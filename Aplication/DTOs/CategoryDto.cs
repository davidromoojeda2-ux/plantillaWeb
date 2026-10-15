using System.ComponentModel.DataAnnotations;

namespace Aplication.DTOs;

/// <summary>DTO de entrada para crear o editar una categoria.</summary>
public class CategoryDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    public string Slug { get; set; } = string.Empty;

    public string? ParentId { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; } = 0;
    public bool ShowInMenu { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
