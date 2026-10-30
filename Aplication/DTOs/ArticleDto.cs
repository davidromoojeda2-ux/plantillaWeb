using System.ComponentModel.DataAnnotations;
using Aplication.DTOs.ContentBlocks;

namespace Aplication.DTOs;

/// <summary>DTO de entrada para crear o editar un articulo editorial.</summary>
public class ArticleDto
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(250, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 250 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    [StringLength(200, ErrorMessage = "El slug no puede superar los 200 caracteres.")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "El resumen es obligatorio.")]
    [StringLength(500, MinimumLength = 20, ErrorMessage = "El resumen debe tener entre 20 y 500 caracteres.")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Cuerpo estructurado; el servicio lo valida y sanitiza.</summary>
    public List<ContentBlockDto> Blocks { get; set; } = [];

    [Required(ErrorMessage = "El autor es obligatorio.")]
    public string Author { get; set; } = string.Empty;

    public string? AuthorBio { get; set; }
    public string? AuthorAvatarUrl { get; set; }
    public string? AuthorRole { get; set; } = "Redacción Especial";
    public string? AuthorSocialUrl { get; set; }

    public string? CategoryId { get; set; }
    public string? SubcategoryId { get; set; }
    public List<string> Tags { get; set; } = [];
    public string? CoverImageId { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<string> ImageIds { get; set; } = [];
    public bool Featured { get; set; } = false;

    /// <summary>"Borrador", "Programado" o "Publicado"</summary>
    public string Status { get; set; } = "Borrador";

    /// <summary>Fecha/hora de publicación cuando el estado es "Programado".</summary>
    public DateTime? ScheduledAt { get; set; }
}
