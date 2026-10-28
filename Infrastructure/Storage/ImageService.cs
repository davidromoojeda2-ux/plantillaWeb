using Aplication.DTOs;
using Aplication.Exceptions;
using Aplication.Interfaces;
using Aplication.Mappers;
using Domine.Entities;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Infrastructure.Storage;

/// <summary>
/// Servicio de optimización y gestión de imágenes.
/// Verifica la disponibilidad del servicio S3 (prioritario) frente a storage local
/// y gestiona el ciclo de vida de imágenes y sus variantes WebP optimizadas.
/// </summary>
public class ImageService
{
    private static readonly int[] TargetWidths = [400, 800, 1600];
    private readonly S3Storage _s3Storage;
    private readonly LocalStorage _localStorage;
    private readonly IImageMetadataRepository _metadataRepo;
    private readonly IArticleRepository? _articleRepo;
    private readonly ILogger<ImageService> _logger;

    public ImageService(
        S3Storage s3Storage,
        LocalStorage localStorage,
        IImageMetadataRepository metadataRepo,
        ILogger<ImageService> logger,
        IArticleRepository? articleRepo = null)
    {
        _s3Storage = s3Storage;
        _localStorage = localStorage;
        _metadataRepo = metadataRepo;
        _logger = logger;
        _articleRepo = articleRepo;
    }

    /// <summary>
    /// Verifica el estado de disponibilidad de los almacenamientos (S3 prioritario y Local fallback).
    /// </summary>
    public async Task<StorageStatusDto> GetStorageStatusAsync(CancellationToken ct = default)
    {
        var isS3 = await _s3Storage.IsAvailableAsync(ct);
        var isLocal = await _localStorage.IsAvailableAsync(ct);

        return new StorageStatusDto
        {
            IsS3Available = isS3,
            IsLocalAvailable = isLocal,
            S3Bucket = _s3Storage.BucketName,
            S3PublicDomain = _s3Storage.PublicDomain,
            LocalStoragePath = _localStorage.BasePath
        };
    }

    /// <summary>
    /// Verifica si ya existe una imagen registrada con el nombre indicado (no sensible a mayúsculas).
    /// </summary>
    public async Task<bool> ExistsByNameAsync(string fileName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        return await _metadataRepo.ExistsByNameAsync(fileName.Trim(), ct);
    }

    /// <summary>
    /// Procesa una imagen subida, verifica no duplicidad de nombre, comprueba la disponibilidad
    /// de S3 (prioritario) frente a LocalStorage, genera variantes WebP optimizadas y las almacena.
    /// </summary>
    /// <param name="originalFileName">Nombre original del archivo para trazabilidad.</param>
    /// <param name="inputStream">Stream con los bytes de la imagen original.</param>
    /// <param name="alt">Texto alternativo de accesibilidad.</param>
    /// <param name="articleId">ID opcional del artículo a vincular.</param>
    /// <param name="customName">Nombre personalizado opcional si se desea renombrar.</param>
    /// <returns>El documento <see cref="ImageMetadata"/> persistido en MongoDB.</returns>
    public async Task<ImageMetadata> ProcessAndStoreAsync(
        string originalFileName,
        Stream inputStream,
        string alt,
        string? articleId = null,
        string? customName = null,
        CancellationToken ct = default)
    {
        var effectiveName = !string.IsNullOrWhiteSpace(customName)
            ? customName.Trim()
            : originalFileName.Trim();

        // 1. Verificación de nombre no repetido
        if (await _metadataRepo.ExistsByNameAsync(effectiveName, ct))
        {
            throw new ConflictException(
                $"Ya existe una imagen registrada con el nombre \"{effectiveName}\". Por favor renombra el archivo o utiliza una imagen con un nombre diferente.",
                nameof(originalFileName));
        }

        // 2. Verificar disponibilidad del servicio S3 (prioritario) frente a storage local
        IImageStorage targetStorage;
        string storageType;

        bool isS3Available = await _s3Storage.IsAvailableAsync(ct);
        if (isS3Available)
        {
            targetStorage = _s3Storage;
            storageType = _s3Storage.StorageType; // "s3" o "r2"
            _logger.LogInformation("Servicio S3 verificado y disponible (prioritario). Se almacenará en S3 (Bucket: {Bucket}).", _s3Storage.BucketName);
        }
        else
        {
            targetStorage = _localStorage;
            storageType = "local";
            _logger.LogWarning("Servicio S3 no disponible. Se almacena en Storage Local como fallback.");
        }

        // 3. Procesamiento y compresión con ImageSharp
        using var image = await Image.LoadAsync(inputStream, ct);

        // Corregir orientación EXIF y limpiar metadatos para privacidad
        image.Mutate(x => x.AutoOrient());
        int originalWidth = image.Width;
        int originalHeight = image.Height;
        image.Metadata.ExifProfile = null;

        var variants = new List<ImageVariant>();
        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM");
        var rawName = Path.GetFileNameWithoutExtension(effectiveName).ToLowerInvariant();
        var cleanName = System.Text.RegularExpressions.Regex.Replace(rawName, @"[^a-z0-9_-]", "-");
        cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"-+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(cleanName)) cleanName = "img";

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var encoder = new WebpEncoder { Quality = 80 };

        // 4. Generación y subida de variantes WebP (400, 800, 1600 px)
        foreach (var width in TargetWidths)
        {
            // Solo generar variante si la imagen original es suficientemente grande (excepto miniatura mínima)
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

            await targetStorage.UploadAsync(key, ms, "image/webp", ct);

            variants.Add(new ImageVariant
            {
                Width = width,
                Key = key,
                SizeBytes = sizeBytes,
                PublicUrl = targetStorage.GetPublicUrl(key)
            });
        }

        var metadata = new ImageMetadata
        {
            OriginalFileName = effectiveName,
            Alt = string.IsNullOrWhiteSpace(alt) ? Path.GetFileNameWithoutExtension(effectiveName) : alt,
            OriginalWidth = originalWidth,
            OriginalHeight = originalHeight,
            Variants = variants,
            Storage = storageType,
            ArticleId = string.IsNullOrWhiteSpace(articleId) ? null : articleId,
            UploadedAt = DateTime.UtcNow
        };

        await _metadataRepo.InsertAsync(metadata);
        _logger.LogInformation("Imagen '{FileName}' procesada exitosamente con {Count} variantes en storage {Storage}.", effectiveName, variants.Count, storageType);

        return metadata;
    }

    public async Task<ImageMetadataResponseDto> ProcessAndStoreDtoAsync(
        string originalFileName,
        Stream inputStream,
        string alt,
        string? articleId = null,
        string? customName = null,
        CancellationToken ct = default)
    {
        var metadata = await ProcessAndStoreAsync(originalFileName, inputStream, alt, articleId, customName, ct);
        return ImageMapper.ToDto(metadata);
    }

    /// <summary>
    /// Consulta paginada con filtros para la biblioteca del CMS.
    /// </summary>
    public async Task<(List<ImageMetadataResponseDto> Items, long TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        string? storage = null,
        string? articleId = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 30;

        var skip = (page - 1) * pageSize;
        var totalCount = await _metadataRepo.CountAsync(search, storage, articleId, ct);
        var entities = await _metadataRepo.GetPagedAsync(skip, pageSize, search, storage, articleId, ct);

        return (ImageMapper.ToDtoList(entities), totalCount);
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

    public async Task<List<ImageMetadataResponseDto>> GetByArticleIdAsync(string articleId) =>
        ImageMapper.ToDtoList(await _metadataRepo.GetByArticleIdAsync(articleId));

    /// <summary>
    /// Elimina una imagen por su ID verificando existencia y eliminando todas sus variantes.
    /// </summary>
    public async Task DeleteByIdAsync(string id, CancellationToken ct = default)
    {
        var metadata = await _metadataRepo.GetByIdAsync(id);
        if (metadata == null)
        {
            throw new NotFoundException($"No se encontró la imagen con ID '{id}'.");
        }

        await DeleteAsync(metadata, ct);
    }

    /// <summary>
    /// Elimina todas las variantes físicas/nube del almacenamiento correspondiente,
    /// desvincula la imagen de artículos asociados y elimina el documento de metadatos.
    /// </summary>
    public async Task DeleteAsync(ImageMetadata metadata, CancellationToken ct = default)
    {
        var primaryStorage = ResolveStorage(metadata.Storage);

        // 1. Eliminar cada una de las variantes del almacenamiento correspondiente
        foreach (var variant in metadata.Variants)
        {
            if (string.IsNullOrWhiteSpace(variant.Key)) continue;

            try
            {
                await primaryStorage.DeleteAsync(variant.Key, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al eliminar variante '{Key}' de {Storage}: {Message}", variant.Key, metadata.Storage, ex.Message);
            }

            // Si la imagen estaba en S3, verificar también en LocalStorage por si hubo sincronización/migración
            if (primaryStorage != _localStorage)
            {
                try
                {
                    await _localStorage.DeleteAsync(variant.Key, ct);
                }
                catch { /* Ignorar si no existe */ }
            }
        }

        // 2. Desvincular de artículos que la referencien
        if (_articleRepo != null)
        {
            try
            {
                if (!string.IsNullOrEmpty(metadata.ArticleId))
                {
                    var article = await _articleRepo.GetByIdAsync(metadata.ArticleId);
                    if (article != null)
                    {
                        bool modified = false;
                        if (article.CoverImageId == metadata.Id)
                        {
                            article.CoverImageId = null;
                            article.CoverImageUrl = null;
                            modified = true;
                        }
                        if (article.ImageIds.Remove(metadata.Id))
                        {
                            modified = true;
                        }
                        if (modified)
                        {
                            await _articleRepo.UpdateAsync(article);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al desvincular imagen '{Id}' de artículos: {Message}", metadata.Id, ex.Message);
            }
        }

        // 3. Eliminar documento de metadatos en MongoDB
        await _metadataRepo.DeleteAsync(metadata.Id);
        _logger.LogInformation("Imagen '{Id}' ({FileName}) y sus variantes eliminadas correctamente.", metadata.Id, metadata.OriginalFileName);
    }

    private IImageStorage ResolveStorage(string storageType)
    {
        if (string.Equals(storageType, "local", StringComparison.OrdinalIgnoreCase))
        {
            return _localStorage;
        }

        // S3 o R2
        return _s3Storage;
    }
}
