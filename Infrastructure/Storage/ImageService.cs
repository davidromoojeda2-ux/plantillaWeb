using Aplication.DTOs;
using Aplication.DTOs.Images;
using Aplication.Exceptions;
using Aplication.Interfaces;
using Aplication.Mappers;
using Aplication.Services;
using Domine.Entities;
using Domine.Exceptions;
using Microsoft.Extensions.Logging;

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
    private readonly IImageProcessor _imageProcessor;
    private readonly IArticleRepository? _articleRepo;
    private readonly ISiteSettingsService? _siteSettings;
    private readonly ILogger<ImageService> _logger;

    public ImageService(
        S3Storage s3Storage,
        LocalStorage localStorage,
        IImageMetadataRepository metadataRepo,
        IImageProcessor imageProcessor,
        ILogger<ImageService> logger,
        IArticleRepository? articleRepo = null,
        ISiteSettingsService? siteSettings = null)
    {
        _s3Storage = s3Storage;
        _localStorage = localStorage;
        _metadataRepo = metadataRepo;
        _imageProcessor = imageProcessor;
        _logger = logger;
        _articleRepo = articleRepo;
        _siteSettings = siteSettings;
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

        // 3. Procesamiento y compresión mediante la estrategia configurada.
        //    La implementación concreta (Magick.NET) queda aislada en Infrastructure.
        var quality = _siteSettings is null
            ? new ImageQualitySettingsDto()
            : await _siteSettings.GetImageQualityAsync();

        var options = new ImageProcessingOptions(
            TargetWidths: TargetWidths,
            QualityByWidth: TargetWidths.ToDictionary(
                width => width,
                width => quality.ForWidth(width)),
            DefaultQuality: quality.Quality,
            OutputFormat: ImageOutputFormat.WebP);

        _logger.LogInformation(
            "Calidad WebP aplicada a '{FileName}': global {Global}, 400px {Small}, 800px {Medium}, 1600px {Large}.",
            effectiveName,
            quality.Quality,
            quality.ForWidth(400),
            quality.ForWidth(800),
            quality.ForWidth(1600));

        var processed = await _imageProcessor.ProcessAsync(inputStream, options, ct);
        var originalWidth = processed.OriginalWidth;
        var originalHeight = processed.OriginalHeight;

        var variants = new List<ImageVariant>();
        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM");
        var rawName = Path.GetFileNameWithoutExtension(effectiveName).ToLowerInvariant();
        var cleanName = System.Text.RegularExpressions.Regex.Replace(rawName, @"[^a-z0-9_-]", "-");
        cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"-+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(cleanName)) cleanName = "img";

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        // 4. Subida de las variantes generadas (400, 800, 1600 px)
        foreach (var processedVariant in processed.Variants)
        {
            var extension = string.Equals(
                processedVariant.ContentType, "image/avif", StringComparison.OrdinalIgnoreCase)
                ? "avif"
                : "webp";

            var key = $"images/{dateFolder}/{cleanName}-{uniqueSuffix}-{processedVariant.RequestedWidth}.{extension}";
            using var ms = new MemoryStream(processedVariant.Bytes);

            await targetStorage.UploadAsync(key, ms, processedVariant.ContentType, ct);

            variants.Add(new ImageVariant
            {
                Width = processedVariant.RequestedWidth,
                Key = key,
                SizeBytes = processedVariant.Bytes.Length,
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
        var idList = ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (idList.Count == 0) return [];

        var images = await _metadataRepo.GetByIdsAsync(idList);
        return images.ToDictionary(img => img.Id, ImageMapper.ToDto);
    }

    public Task UpdateAltAsync(string id, string alt) =>
        _metadataRepo.UpdateAltAsync(id, alt);

    /// <summary>
    /// Renombra el nombre visible de una imagen manteniendo intactas sus variantes y URLs públicas.
    /// </summary>
    public async Task<ImageMetadataResponseDto> RenameAsync(string id, string newName, CancellationToken ct = default)
    {
        var metadata = await _metadataRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró la imagen con ID '{id}'.");

        var fileName = (newName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainValidationException("El nombre de la imagen es obligatorio.", nameof(newName));
        }

        // Si el usuario no incluye extensión, se conserva la del nombre actual.
        if (!Path.HasExtension(fileName) && Path.HasExtension(metadata.OriginalFileName))
        {
            fileName += Path.GetExtension(metadata.OriginalFileName);
        }

        if (!string.Equals(fileName, metadata.OriginalFileName, StringComparison.OrdinalIgnoreCase) &&
            await _metadataRepo.ExistsByNameAsync(fileName, ct))
        {
            throw new ConflictException(
                $"Ya existe una imagen registrada con el nombre \"{fileName}\". Elige un nombre diferente.",
                nameof(newName));
        }

        await _metadataRepo.UpdateFileNameAsync(id, fileName);
        metadata.OriginalFileName = fileName;

        return ImageMapper.ToDto(metadata);
    }

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

        // 2. Desvincular de todos los artículos que la referencien (portada o galería)
        if (_articleRepo != null)
        {
            try
            {
                var articles = await _articleRepo.GetAllAsync(0, 5000);
                foreach (var article in articles)
                {
                    bool modified = false;

                    if (article.CoverImageId == metadata.Id)
                    {
                        article.CoverImageId = null;
                        article.CoverImageUrl = null;
                        modified = true;
                    }

                    if (article.ImageIds is not null && article.ImageIds.Remove(metadata.Id))
                    {
                        modified = true;
                    }

                    if (modified)
                    {
                        await _articleRepo.UpdateAsync(article);
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
