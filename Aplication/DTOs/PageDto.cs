using System.ComponentModel.DataAnnotations;

namespace Aplication.DTOs;

/// <summary>DTO de entrada para crear o editar una pagina informativa institucional.</summary>
public class PageDto
{
    [Required(ErrorMessage = "El titulo es obligatorio.")]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "El contenido es obligatorio.")]
    public string Content { get; set; } = string.Empty;

    public int Order { get; set; } = 0;
    public bool ShowInFooter { get; set; } = true;
}

/// <summary>DTO de respuesta de una pagina informativa.</summary>
public class PageResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool ShowInFooter { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
