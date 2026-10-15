namespace Aplication.DTOs;

/// <summary>DTO de respuesta de un comentario aprobado o moderado.</summary>
public class CommentResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string ArticleId { get; set; } = string.Empty;
    public string? VisitorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
