using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Mappers;
using Domine.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Infrastructure.Storage;

/// <summary>
/// Servicio de procesamiento de imágenes con SixLabors.ImageSharp.
/// </summary>
public class ImageService
{
    private static readonly int[] TargetWidths = [400, 800, 1600];
    private readonly IImageStorage _storage;
    private readonly IImageMetadataRepository _metadataRepo;
    private readonly IServiceProvider? _serviceProvider;

    public ImageService(
        IImageStorage storage,
        IImageMetadataRepository metadataRepo,
        IServiceProvider? serviceProvider = null)
    {
        _storage = storage;
        _metadataRepo = metadataRepo;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Procesa una imagen subida, genera variantes WebP y las almacena.
    /// </summary>
    /// <param name="originalFileName">Nombre original del archivo para trazabilidad.</param>
    /// <param name="inputStream">Stream con los bytes de la imagen original.</param>
    /// <param name="alt">Texto alternativo de accesibilidad.</param>
    /// <returns>El documento <see cref="ImageMetadata"/> persistido en MongoDB.</returns>
    public async Task<ImageMetadata> ProcessAndStoreAsync(
        string originalFileName, Stream inputStream, string alt, string? articleId = null, CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(inputStream, ct);

        // Corregir orientación EXIF
        image.Mutate(x => x.AutoOrient());

        int originalWidth = image.Width;
        int originalHeight = image.Height;

        // Limpiar metadatos EXIF para privacidad
        image.Metadata.ExifProfile = null;

        var variants = new List<ImageVariant>();
        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM");
        var rawName = Path.GetFileNameWithoutExtension(originalFileName).ToLowerInvariant();
        var cleanName = System.Text.RegularExpressions.Regex.Replace(rawName, @"[^a-z0-9_-]", "-");
        cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"-+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(cleanName)) cleanName = "img";

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var encoder = new WebpEncoder { Quality = 80 };

        foreach (var width in TargetWidths)
        {
            // Solo generar variante si la imagen fuente es suficientemente ancha
            if (image.Width < width && width != TargetWidths[0]) continue;

            using var variant = image.Clone(ctx =>
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(Math.Min(width, image.Width), 0),
                    Mode = ResizeMode.Max
                });
            });

            var key = $"images/{dateFolder}/{cleanName}-{uniqueSuffix}-{width}.webp";
            using var ms = new MemoryStream();
            await variant.SaveAsync(ms, encoder, ct);
            var sizeBytes = ms.Length;
            ms.Position = 0;

            await _storage.UploadAsync(key, ms, "image/webp", ct);

            variants.Add(new ImageVariant
            {
                Width = width,
                Key = key,
                SizeBytes = sizeBytes,
                PublicUrl = _storage.GetPublicUrl(key)
            });
        }

        var metadata = new ImageMetadata
        {
            OriginalFileName = originalFileName,
            Alt = alt,
            OriginalWidth = originalWidth,
            OriginalHeight = originalHeight,
            Variants = variants,
            Storage = _storage is CloudflareR2Storage ? "r2" : "local",
            ArticleId = string.IsNullOrWhiteSpace(articleId) ? null : articleId
        };

        await _metadataRepo.InsertAsync(metadata);
        return metadata;
    }

    public async Task<ImageMetadataResponseDto> ProcessAndStoreDtoAsync(
        string originalFileName, Stream inputStream, string alt, string? articleId = null, CancellationToken ct = default)
    {
        var metadata = await ProcessAndStoreAsync(originalFileName, inputStream, alt, articleId, ct);
        return ImageMapper.ToDto(metadata);
    }

    public async Task<List<ImageMetadataResponseDto>> GetAllAsync(int skip = 0, int take = 50) =>
        ImageMapper.ToDtoList(await _metadataRepo.GetAllAsync(skip, take));

    public async Task<ImageMetadataResponseDto?> GetByIdAsync(string id)
    {
        var img = await _metadataRepo.GetByIdAsync(id);
        return img is not null ? ImageMapper.ToDto(img) : null;
    }

    public async Task<Dictionary<string, ImageMetadataResponseDto>> GetVariantsDictAsync(IEnumerable<string> ids)
    {
        var dict = new Dictionary<string, ImageMetadataResponseDto>();
        foreach (var id in ids.Distinct())
        {
            var img = await _metadataRepo.GetByIdAsync(id);
            if (img != null) dict[id] = ImageMapper.ToDto(img);
        }
        return dict;
    }

    public Task UpdateAltAsync(string id, string alt) =>
        _metadataRepo.UpdateAltAsync(id, alt);

    public Task UpdateArticleIdAsync(string id, string? articleId) =>
        _metadataRepo.UpdateArticleIdAsync(id, articleId);

    public async Task<List<ImageMetadataResponseDto>> GetByArticleIdAsync(string articleId) =>
        ImageMapper.ToDtoList(await _metadataRepo.GetByArticleIdAsync(articleId));

    public async Task DeleteByIdAsync(string id, CancellationToken ct = default)
    {
        var metadata = await _metadataRepo.GetByIdAsync(id);
        if (metadata != null)
            await DeleteAsync(metadata, ct);
    }

    /// <summary>
    /// Elimina todas las variantes del almacenamiento correspondiente y el documento de metadatos.
    /// </summary>
    public async Task DeleteAsync(ImageMetadata metadata, CancellationToken ct = default)
    {
        var targetStorage = ResolveStorage(metadata.Storage);

        foreach (var variant in metadata.Variants)
            await targetStorage.DeleteAsync(variant.Key, ct);

        await _metadataRepo.DeleteAsync(metadata.Id);
    }

    private IImageStorage ResolveStorage(string storageType)
    {
        if (_serviceProvider != null)
        {
            if (string.Equals(storageType, "local", StringComparison.OrdinalIgnoreCase))
            {
                if (_serviceProvider.GetService(typeof(LocalStorage)) is IImageStorage local)
                    return local;
            }
            else if (string.Equals(storageType, "r2", StringComparison.OrdinalIgnoreCase))
            {
                if (_serviceProvider.GetService(typeof(CloudflareR2Storage)) is IImageStorage r2)
                    return r2;
            }
        }

        return _storage;
    }
}
