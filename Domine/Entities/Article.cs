using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.Entities;

/// <summary>Estado de publicación de un artículo.</summary>
public enum ArticleStatus
{
    Borrador,
    Publicado
}

/// <summary>
/// Artículo del portal de noticias. Contiene el contenido editorial completo,
/// metadatos SEO, referencias a imágenes en R2 y estado de publicación.
/// </summary>
public class Article
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Title { get; set; } = string.Empty;

    /// <summary>Slug único para URL, e.g. "inauguran-nuevo-centro-de-innovacion".</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Resumen corto para tarjetas y SEO (meta description).</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Contenido HTML sanitizado del artículo.</summary>
    public string Content { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? CategoryId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? SubcategoryId { get; set; }

    public List<string> Tags { get; set; } = [];

    /// <summary>Id de la imagen de portada en la colección images.</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CoverImageId { get; set; }

    /// <summary>Ids adicionales de imágenes asociadas al artículo.</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> ImageIds { get; set; } = [];

    /// <summary>Si es true, aparece en la sección de destacados de la portada.</summary>
    public bool Featured { get; set; } = false;

    [BsonRepresentation(BsonType.String)]
    public ArticleStatus Status { get; set; } = ArticleStatus.Borrador;

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Contador de vistas acumuladas del artículo.</summary>
    public long ViewCount { get; set; } = 0;

    /// <summary>Tiempo estimado de lectura en minutos (calculado al publicar).</summary>
    public int ReadingTimeMinutes { get; set; } = 1;
}
