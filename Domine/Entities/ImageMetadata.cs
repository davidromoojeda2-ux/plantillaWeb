using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.Entities;

/// <summary>
/// Variante de una imagen (WebP) almacenada en Cloudflare R2 o almacenamiento local.
/// Cada variante corresponde a un ancho específico: 400, 800 o 1600 px.
/// </summary>
public class ImageVariant
{
    /// <summary>Ancho en píxeles de esta variante (e.g. 400, 800, 1600).</summary>
    public int Width { get; set; }

    /// <summary>Clave del objeto en Cloudflare R2, e.g. "images/2025/01/photo-800.webp".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Tamaño en bytes del archivo en R2.</summary>
    public long SizeBytes { get; set; }

    /// <summary>URL pública de acceso CDN para esta variante.</summary>
    public string? PublicUrl { get; set; }
}

/// <summary>
/// Metadatos de una imagen procesada y almacenada en Cloudflare R2.
/// Los binarios viven en R2; este documento registra las variantes y metadatos.
/// </summary>
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

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
