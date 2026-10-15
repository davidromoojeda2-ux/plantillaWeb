using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Domine.Exceptions;

namespace Domine.Entities;

/// <summary>
/// Artículo del portal de noticias. Contiene el contenido editorial completo,
/// metadatos SEO, referencias a imágenes en R2 y estado de publicación.
/// </summary>
[BsonIgnoreExtraElements]
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

    /// <summary>Biografía o descripción del autor.</summary>
    public string? AuthorBio { get; set; }

    /// <summary>URL del avatar o foto del autor.</summary>
    public string? AuthorAvatarUrl { get; set; }

    /// <summary>Rol o cargo editorial del autor, e.g. "Redacción Especial".</summary>
    public string? AuthorRole { get; set; } = "Redacción Especial";

    /// <summary>Enlace a perfil social o red del autor (X, LinkedIn, web personal).</summary>
    public string? AuthorSocialUrl { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? CategoryId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? SubcategoryId { get; set; }

    public List<string> Tags { get; set; } = [];

    /// <summary>Id de la imagen de portada en la colección images.</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CoverImageId { get; set; }

    /// <summary>Ruta o URL directa de la imagen de portada.</summary>
    public string? CoverImageUrl { get; set; }

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

    /// <summary>
    /// Calcula el tiempo estimado de lectura en base al conteo de palabras del contenido HTML.
    /// </summary>
    public static int EstimateReadingTime(string? htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent)) return 1;

        var words = System.Text.RegularExpressions.Regex
            .Replace(htmlContent, "<[^>]+>", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return Math.Max(1, (int)Math.Ceiling(words / 200.0));
    }

    /// <summary>
    /// Valida las invariantes del artículo según las reglas de negocio del dominio.
    /// Lanza <see cref="DomainValidationException"/> si el estado es inconsistente.
    /// </summary>
    public void ValidarInvariantes()
    {
        if (string.IsNullOrWhiteSpace(Title))
            throw new DomainValidationException("El título del artículo es obligatorio.", nameof(Title));

        if (string.IsNullOrWhiteSpace(Slug))
            throw new DomainValidationException("El slug del artículo es obligatorio.", nameof(Slug));

        if (Status == ArticleStatus.Publicado)
        {
            if (string.IsNullOrWhiteSpace(Content))
                throw new DomainValidationException("No se puede publicar un artículo sin contenido.", nameof(Content));
        }

        if (ReadingTimeMinutes < 1)
            ReadingTimeMinutes = 1;
    }

    /// <summary>
    /// Cambia el estado a Publicado, asigna la fecha de publicación si no existía,
    /// recalcula el tiempo de lectura y valida las invariantes del dominio.
    /// </summary>
    public void Publicar()
    {
        Status = ArticleStatus.Publicado;
        PublishedAt ??= DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        ReadingTimeMinutes = EstimateReadingTime(Content);
        ValidarInvariantes();
    }

    /// <summary>
    /// Cambia el estado a Borrador y valida las invariantes del dominio.
    /// </summary>
    public void MarcarBorrador()
    {
        Status = ArticleStatus.Borrador;
        UpdatedAt = DateTime.UtcNow;
        ValidarInvariantes();
    }
}
