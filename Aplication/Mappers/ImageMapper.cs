using Aplication.DTOs;
using Domine.Entities;

namespace Aplication.Mappers;

public static class ImageMapper
{
    // ── Entity → DTO ──────────────────────────────────────────────────────────
    // Estos métodos son usados por Infrastructure (ImageService), no por las vistas.

    public static ImageVariantDto ToDto(ImageVariant variant) => new()
    {
        Width     = variant.Width,
        Key       = variant.Key,
        SizeBytes = variant.SizeBytes,
        PublicUrl = variant.PublicUrl
    };

    public static ImageMetadataResponseDto ToDto(ImageMetadata image) => new()
    {
        Id               = image.Id,
        OriginalFileName = image.OriginalFileName,
        Alt              = image.Alt,
        OriginalWidth    = image.OriginalWidth,
        OriginalHeight   = image.OriginalHeight,
        Variants         = image.Variants.Select(ToDto).ToList(),
        Storage          = image.Storage,
        ArticleId        = image.ArticleId,
        UploadedAt       = image.UploadedAt
    };

    public static List<ImageMetadataResponseDto> ToDtoList(IEnumerable<ImageMetadata> images) =>
        images.Select(ToDto).ToList();

    // ── URL Helpers (operan sobre DTOs, seguros desde cualquier capa) ─────────

    /// <summary>Devuelve la URL pública de la variante de mayor resolución.</summary>
    public static string? GetBestUrl(ImageMetadataResponseDto? image) =>
        image?.Variants.OrderByDescending(v => v.Width).FirstOrDefault()?.PublicUrl;

    /// <summary>Devuelve la URL de la variante más pequeña (miniatura ~400px).</summary>
    public static string? GetThumbnailUrl(ImageMetadataResponseDto? image) =>
        image?.Variants.OrderBy(v => v.Width).FirstOrDefault()?.PublicUrl;

    /// <summary>Devuelve la URL de la variante más cercana al ancho solicitado.</summary>
    public static string? GetUrlForWidth(ImageMetadataResponseDto? image, int targetWidth) =>
        image?.Variants
              .OrderBy(v => Math.Abs(v.Width - targetWidth))
              .FirstOrDefault()?.PublicUrl;
}
