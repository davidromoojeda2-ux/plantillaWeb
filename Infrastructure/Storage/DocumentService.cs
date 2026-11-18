using Aplication.Configuration;
using Aplication.DTOs;
using Aplication.Exceptions;
using Aplication.Interfaces;
using Aplication.Mappers;
using Aplication.Services;
using Domine.Entities;
using Domine.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

/// <summary>
/// Orquesta la validación, el almacenamiento y los metadatos de documentos.
/// Los binarios son privados y solo se entregan a través de la aplicación.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IReadOnlyList<IDocumentStorage> _storages;
    private readonly IDocumentRepository _repository;
    private readonly IArticleRepository _articleRepository;
    private readonly DocumentValidator _validator;
    private readonly DocumentsOptions _options;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IEnumerable<IDocumentStorage> storages,
        IDocumentRepository repository,
        IArticleRepository articleRepository,
        DocumentValidator validator,
        IOptions<DocumentsOptions> options,
        ILogger<DocumentService> logger)
    {
        _storages = storages.ToList();
        _repository = repository;
        _articleRepository = articleRepository;
        _validator = validator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DocumentResponseDto> UploadAsync(DocumentUploadRequest request, CancellationToken ct = default)
    {
        if (request.Content is null)
            throw new DomainValidationException("No se ha proporcionado ningún archivo.", nameof(request.Content));

        using var buffer = new MemoryStream();
        await CopyWithLimitAsync(request.Content, buffer, _options.MaxSizeBytes, ct);
        buffer.Position = 0;

        var validated = _validator.Validate(request.OriginalFileName, request.DisplayName, buffer);
        var key = BuildKey(validated);

        var target = await ResolveUploadStorageAsync(ct);
        var storageType = target.StorageType;

        try
        {
            await target.UploadAsync(key, buffer, validated.ContentType, ct);
        }
        catch (Exception ex) when (!string.Equals(storageType, "local", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(ex,
                "No se pudo subir el documento a {Storage}; se usará almacenamiento local. {Message}",
                storageType, ex.Message);

            target = ResolveLocalStorage();
            storageType = target.StorageType;
            buffer.Position = 0;
            await target.UploadAsync(key, buffer, validated.ContentType, ct);
        }

        var document = new DocumentMetadata
        {
            OriginalFileName = validated.OriginalFileName,
            DisplayName = validated.DisplayName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Extension = validated.Extension,
            ContentType = validated.ContentType,
            SizeBytes = validated.SizeBytes,
            Sha256 = validated.Sha256,
            Storage = storageType,
            Key = key,
            IsPublic = request.IsPublic,
            UploadedAt = DateTime.UtcNow
        };

        try
        {
            await _repository.InsertAsync(document);
        }
        catch
        {
            try
            {
                await target.DeleteAsync(key, ct);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx,
                    "No se pudo eliminar el documento huérfano {Key} tras fallar la persistencia: {Message}",
                    key, cleanupEx.Message);
            }

            throw;
        }

        _logger.LogInformation(
            "Documento {DocumentId} ({FileName}) almacenado en {Storage} con clave {Key}.",
            document.Id, document.OriginalFileName, storageType, key);

        return DocumentMapper.ToDto(document);
    }

    public async Task<(List<DocumentResponseDto> Items, long Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        string? storage = null,
        string? extension = null,
        string? articleFilter = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var skip = (page - 1) * pageSize;
        var total = await _repository.CountAsync(search, storage, extension, articleFilter, ct);
        var items = await _repository.GetPagedAsync(skip, pageSize, search, storage, extension, articleFilter, ct);

        return (DocumentMapper.ToDtoList(items), total);
    }

    public async Task<List<DocumentResponseDto>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default) =>
        DocumentMapper.ToDtoList(await _repository.GetAllAsync(skip, take));

    public async Task<DocumentResponseDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id) || !MongoDB.Bson.ObjectId.TryParse(id, out _))
            return null;

        var document = await _repository.GetByIdAsync(id);
        return document is null ? null : DocumentMapper.ToDto(document);
    }

    public async Task<List<DocumentResponseDto>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var idList = ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (idList.Count == 0) return [];

        var documents = await _repository.GetByIdsAsync(idList);
        return DocumentMapper.ToDtoList(documents);
    }

    public async Task<DocumentResponseDto> UpdateMetadataAsync(
        string id,
        string displayName,
        string? description,
        bool isPublic,
        CancellationToken ct = default)
    {
        var document = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró el documento con ID '{id}'.");

        var normalizedName = DocumentValidator.SanitizeDisplayName(displayName, document.OriginalFileName);
        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new DomainValidationException("El nombre visible es obligatorio.", nameof(displayName));

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription?.Length > 1000)
            throw new DomainValidationException("La descripción no puede superar los 1000 caracteres.", nameof(description));

        await _repository.UpdateMetadataAsync(id, normalizedName, normalizedDescription, isPublic);

        document.DisplayName = normalizedName;
        document.Description = normalizedDescription;
        document.IsPublic = isPublic;

        return DocumentMapper.ToDto(document);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var document = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No se encontró el documento con ID '{id}'.");

        var storage = ResolveStorage(document.Storage);
        if (storage is not null)
        {
            try
            {
                await storage.DeleteAsync(document.Key, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar el objeto {Key} de {Storage}: {Message}",
                    document.Key, document.Storage, ex.Message);
            }

            foreach (var other in _storages.Where(s => !ReferenceEquals(s, storage)))
            {
                try
                {
                    await other.DeleteAsync(document.Key, ct);
                }
                catch
                {
                    // El objeto solo vive en uno de los proveedores; ignorar ausencias.
                }
            }
        }

        try
        {
            var articles = await _articleRepository.GetAllAsync(0, 5000);
            foreach (var article in articles)
            {
                if (article.DocumentIds is not null && article.DocumentIds.Remove(id))
                {
                    await _articleRepository.UpdateAsync(article);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo desvincular el documento {DocumentId} de los artículos: {Message}",
                document.Id, ex.Message);
        }

        await _repository.DeleteAsync(id);
        _logger.LogInformation("Documento {DocumentId} ({FileName}) eliminado correctamente.",
            document.Id, document.OriginalFileName);
    }

    public async Task<bool> CanDownloadPublicAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id) || !MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;

        var document = await _repository.GetByIdAsync(id);
        if (document is null) return false;
        if (document.IsPublic) return true;

        return await _articleRepository.IsDocumentReferencedByPublishedArticleAsync(id, ct);
    }

    public async Task<DocumentDownloadResult?> OpenDownloadAsync(
        string id,
        bool requirePublic,
        long? offset = null,
        long? length = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id) || !MongoDB.Bson.ObjectId.TryParse(id, out _))
            return null;

        var document = await _repository.GetByIdAsync(id);
        if (document is null) return null;

        if (requirePublic && !await CanDownloadPublicAsync(id, ct))
            return null;

        var storage = ResolveStorage(document.Storage);
        var stream = storage is null
            ? null
            : await storage.OpenReadAsync(document.Key, offset, length, ct);

        if (stream is null) return null;

        return new DocumentDownloadResult(
            stream,
            BuildDownloadFileName(document),
            document.ContentType,
            document.SizeBytes,
            document.Sha256,
            document.UploadedAt,
            document.IsPublic);
    }

    public async Task IncrementDownloadCountAsync(string id, CancellationToken ct = default)
    {
        try
        {
            await _repository.IncrementDownloadCountAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo incrementar el contador de descargas del documento {DocumentId}: {Message}",
                id, ex.Message);
        }
    }

    private async Task<IDocumentStorage> ResolveUploadStorageAsync(CancellationToken ct)
    {
        var cloud = CloudStorage;
        if (cloud is not null && await cloud.IsAvailableAsync(ct))
            return cloud;

        return ResolveLocalStorage();
    }

    private IDocumentStorage? CloudStorage =>
        _storages.FirstOrDefault(storage =>
            storage.StorageType.Equals("s3", StringComparison.OrdinalIgnoreCase) ||
            storage.StorageType.Equals("r2", StringComparison.OrdinalIgnoreCase));

    private IDocumentStorage ResolveLocalStorage() =>
        _storages.FirstOrDefault(storage =>
            storage.StorageType.Equals("local", StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException("No hay un almacenamiento local de documentos configurado.");

    private IDocumentStorage? ResolveStorage(string storageType)
    {
        if (storageType.Equals("local", StringComparison.OrdinalIgnoreCase))
            return _storages.FirstOrDefault(storage =>
                storage.StorageType.Equals("local", StringComparison.OrdinalIgnoreCase));

        return CloudStorage;
    }

    private static string BuildKey(ValidatedDocument document)
    {
        var now = DateTime.UtcNow;
        return $"documents/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}-{document.StorageSlug}{document.Extension}";
    }

    private static string BuildDownloadFileName(DocumentMetadata document)
    {
        var name = string.IsNullOrWhiteSpace(document.DisplayName)
            ? document.OriginalFileName
            : document.DisplayName;

        if (!Path.HasExtension(name))
            name += document.Extension;

        return DocumentValidator.SanitizeFileName(name);
    }

    private static async Task CopyWithLimitAsync(
        Stream source,
        MemoryStream destination,
        long maxSizeBytes,
        CancellationToken ct)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;

        while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            total += read;
            if (total > maxSizeBytes)
            {
                var maxMb = maxSizeBytes / (1024d * 1024d);
                throw new DomainValidationException(
                    $"El archivo supera el tamaño máximo permitido ({maxMb:0.#} MB).",
                    "file");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), ct);
        }
    }
}
