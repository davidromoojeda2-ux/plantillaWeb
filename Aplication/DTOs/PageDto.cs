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

    /// <summary>Texto breve que aparece bajo el título en la cabecera de la página pública.</summary>
    [StringLength(300)]
    public string Subtitle { get; set; } = string.Empty;

    public int Order { get; set; } = 0;
    public bool ShowInFooter { get; set; } = true;
}
