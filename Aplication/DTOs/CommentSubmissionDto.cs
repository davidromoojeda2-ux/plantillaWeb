using System.ComponentModel.DataAnnotations;

namespace Aplication.DTOs;

/// <summary>DTO de envio de comentario por parte del visitante (incluye honeypot).</summary>
public class CommentSubmissionDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato valido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El comentario es obligatorio.")]
    [StringLength(2000, MinimumLength = 10)]
    public string Content { get; set; } = string.Empty;

    /// <summary>Campo honeypot antispam; debe enviarse vacio por clientes legitimos.</summary>
    public string? Trap { get; set; }

    [Required]
    public string ArticleId { get; set; } = string.Empty;
}
