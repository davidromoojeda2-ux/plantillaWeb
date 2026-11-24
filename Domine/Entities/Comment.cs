using Domine.Common;

namespace Domine.Entities;

/// <summary>
/// Comentario enviado por un visitante sobre un artículo.
/// Incluye protección honeypot y flujo de moderación.
/// </summary>
public class Comment
{
    public string Id { get; set; } = EntityId.New();

    public string ArticleId { get; set; } = string.Empty;

    public string? VisitorId { get; set; }

    /// <summary>Nombre visible del comentarista.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Texto del comentario.</summary>
    public string Content { get; set; } = string.Empty;

    public CommentStatus Status { get; set; } = CommentStatus.Pendiente;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
