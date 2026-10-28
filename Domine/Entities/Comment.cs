using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.Entities;

/// <summary>
/// Comentario enviado por un visitante sobre un artículo.
/// Incluye protección honeypot y flujo de moderación.
/// </summary>
[BsonIgnoreExtraElements]
public class Comment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string ArticleId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? VisitorId { get; set; }

    /// <summary>Nombre visible del comentarista.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Texto del comentario.</summary>
    public string Content { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public CommentStatus Status { get; set; } = CommentStatus.Pendiente;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
