using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Domine.Content;
using Domine.ContentBlocks;
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

    /// <summary>Cuerpo editorial estructurado y ya sanitizado.</summary>
    public List<ContentBlock> Blocks { get; set; } = [];

    /// <summary>Versión del esquema de bloques con la que se guardó el documento.</summary>
    public int SchemaVersion { get; set; } = ContentLimits.CurrentSchemaVersion;

    /// <summary>Texto plano denormalizado para el índice de búsqueda.</summary>
    public string SearchText { get; set; } = string.Empty;

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

    /// <summary>Tiempo estimado de lectura en minutos (calculado desde los bloques).</summary>
    public int ReadingTimeMinutes { get; set; } = 1;

    /// <summary>
    /// Construye el texto plano de búsqueda a partir de todos los bloques.
    /// Nunca incluye marcado HTML.
    /// </summary>
    public string BuildSearchText()
    {
        var parts = new List<string>();

        foreach (var block in Blocks)
        {
            switch (block)
            {
                case ContentParagraphBlock paragraph:
                    parts.Add(paragraph.Text.Text);
                    break;
                case ContentHeadingBlock heading:
                    parts.Add(heading.Text.Value);
                    break;
                case ContentOrderedListBlock ordered:
                    parts.AddRange(ordered.Items.Select(i => i.Text));
                    break;
                case ContentBulletListBlock bullets:
                    parts.AddRange(bullets.Items.Select(i => i.Text));
                    break;
                case ContentQuoteBlock quote:
                    parts.Add(quote.Text.Text);
                    if (quote.Attribution is not null) parts.Add(quote.Attribution.Value);
                    break;
                case ContentCalloutBlock callout:
                    if (callout.Title is not null) parts.Add(callout.Title.Value);
                    parts.Add(callout.Text.Text);
                    break;
                case ContentImageBlock image:
                    parts.Add(image.Alt.Value);
                    if (image.Caption is not null) parts.Add(image.Caption.Value);
                    if (image.Credit is not null) parts.Add(image.Credit.Value);
                    break;
                case ContentTableBlock table:
                    parts.AddRange(table.Headers.Select(h => h.Value));
                    if (table.Caption is not null) parts.Add(table.Caption.Value);
                    foreach (var row in table.Rows)
                        parts.AddRange(row.Select(c => c.Value));
                    break;
                case ContentCodeBlock code:
                    parts.Add(code.Code.Value);
                    break;
            }
        }

        return string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
    }

    /// <summary>
    /// Calcula el tiempo estimado de lectura a partir del texto plano.
    /// </summary>
    public static int EstimateReadingTime(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return 1;

        var words = plainText
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Length;

        return Math.Max(1, (int)Math.Ceiling(words / 200.0));
    }

    public static int EstimateReadingTime(IEnumerable<ContentBlock> blocks)
    {
        var text = string.Join(' ', ExtractPlainText(blocks));
        return EstimateReadingTime(text);
    }

    private static IEnumerable<string> ExtractPlainText(IEnumerable<ContentBlock> blocks)
    {
        var article = new Article { Blocks = [.. blocks] };
        var text = article.BuildSearchText();
        return text.Length == 0 ? [] : [text];
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

        if (Blocks.Count > ContentLimits.MaxBlocks)
            throw new DomainValidationException($"El artículo no puede superar los {ContentLimits.MaxBlocks} bloques.", nameof(Blocks));

        if (Status == ArticleStatus.Publicado && Blocks.Count == 0)
            throw new DomainValidationException("No se puede publicar un artículo sin contenido.", nameof(Blocks));

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
        SearchText = BuildSearchText();
        ReadingTimeMinutes = EstimateReadingTime(Blocks);
        ValidarInvariantes();
    }

    /// <summary>
    /// Cambia el estado a Borrador y valida las invariantes del dominio.
    /// </summary>
    public void MarcarBorrador()
    {
        Status = ArticleStatus.Borrador;
        UpdatedAt = DateTime.UtcNow;
        SearchText = BuildSearchText();
        ReadingTimeMinutes = EstimateReadingTime(Blocks);
        ValidarInvariantes();
    }
}
