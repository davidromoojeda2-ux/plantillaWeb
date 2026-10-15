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
