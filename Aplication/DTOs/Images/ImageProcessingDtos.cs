namespace Aplication.DTOs.Images;

/// <summary>Formatos de salida admitidos por el procesador de imágenes.</summary>
public enum ImageOutputFormat
{
    WebP,
    Avif
}

/// <summary>
/// Parámetros de procesamiento. No dependen de ninguna librería concreta: el
/// adaptador de infraestructura decide cómo generarlos.
/// </summary>
public sealed record ImageProcessingOptions(
    IReadOnlyList<int> TargetWidths,
    IReadOnlyDictionary<int, int> QualityByWidth,
    int DefaultQuality = 80,
    ImageOutputFormat OutputFormat = ImageOutputFormat.WebP)
{
    public int QualityFor(int width) =>
        QualityByWidth.TryGetValue(width, out var quality)
            ? Math.Clamp(quality, 1, 100)
            : Math.Clamp(DefaultQuality, 1, 100);
}

/// <summary>Variante generada por el procesador.</summary>
public sealed record ProcessedImageVariant(
    int RequestedWidth,
    int Width,
    int Height,
    byte[] Bytes,
    string ContentType);

/// <summary>Resultado completo del procesamiento de una imagen.</summary>
public sealed record ProcessedImage(
    int OriginalWidth,
    int OriginalHeight,
    IReadOnlyList<ProcessedImageVariant> Variants);
