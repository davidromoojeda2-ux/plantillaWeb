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

    /// <summary>
    /// Construye el atributo <c>srcset</c> con todas las variantes WebP disponibles,
    /// ordenadas de menor a mayor ancho. Devuelve <c>null</c> si no hay variantes utilizables.
    /// </summary>
    public static string? GetSrcSet(ImageMetadataResponseDto? image)
    {
        if (image?.Variants is null || image.Variants.Count == 0) return null;

        var sources = image.Variants
            .Where(v => !string.IsNullOrWhiteSpace(v.PublicUrl))
            .OrderBy(v => v.Width)
            .Select(v => $"{v.PublicUrl} {v.Width}w")
            .ToList();

        return sources.Count > 0 ? string.Join(", ", sources) : null;
    }

    /// <summary>
    /// Construye el <c>srcset</c> solo cuando la URL preferida pertenece a una de las
    /// variantes registradas (o no se ha indicado URL). Si la URL es externa, devuelve
    /// <c>null</c> para no mezclar orígenes distintos en el mismo atributo.
    /// </summary>
    public static string? GetSrcSetForUrl(ImageMetadataResponseDto? image, string? preferredUrl)
    {
        if (image is null) return null;
        if (string.IsNullOrWhiteSpace(preferredUrl)) return GetSrcSet(image);

        var isRegisteredVariant = image.Variants.Any(v =>
            !string.IsNullOrWhiteSpace(v.PublicUrl) &&
            string.Equals(v.PublicUrl, preferredUrl, StringComparison.OrdinalIgnoreCase));

        return isRegisteredVariant ? GetSrcSet(image) : null;
    }

    /// <summary>
    /// Resuelve la URL de una tarjeta priorizando la portada explícita del artículo
    /// y, si no existe, la variante optimizada de la imagen asociada.
    /// </summary>
    public static string? GetCardUrl(ArticleResponseDto? article, ImageMetadataResponseDto? image) =>
        !string.IsNullOrWhiteSpace(article?.CoverImageUrl)
            ? article.CoverImageUrl
            : GetThumbnailUrl(image) ?? GetBestUrl(image);

    /// <summary>Construye el diccionario de miniaturas usado por los listados administrativos.</summary>
    public static Dictionary<string, string> ToThumbnailDictionary(
        IReadOnlyDictionary<string, ImageMetadataResponseDto> images) =>
        images.ToDictionary(
            kvp => kvp.Key,
            kvp => GetThumbnailUrl(kvp.Value) ?? GetBestUrl(kvp.Value) ?? string.Empty);
}
