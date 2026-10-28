using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.Entities;

/// <summary>
/// Metadatos de una imagen procesada y almacenada en Cloudflare R2.
/// Los binarios viven en R2; este documento registra las variantes y metadatos.
/// </summary>
[BsonIgnoreExtraElements]
public class ImageMetadata
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>Nombre original del archivo subido.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Texto alternativo de accesibilidad.</summary>
    public string Alt { get; set; } = string.Empty;

    /// <summary>Ancho original en píxeles de la imagen fuente.</summary>
    public int OriginalWidth { get; set; }

    /// <summary>Alto original en píxeles de la imagen fuente.</summary>
    public int OriginalHeight { get; set; }

    /// <summary>Variantes WebP generadas (400, 800, 1600 px).</summary>
    public List<ImageVariant> Variants { get; set; } = [];

    /// <summary>Proveedor de almacenamiento: "r2" o "local".</summary>
    public string Storage { get; set; } = "r2";

    /// <summary>Id del artículo al cual está vinculada la imagen (opcional).</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ArticleId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
