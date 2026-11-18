using Aplication.DTOs;

namespace Aplication.Interfaces;

/// <summary>Casos de uso de la biblioteca de documentos y su descarga controlada.</summary>
public interface IDocumentService
{
    Task<DocumentResponseDto> UploadAsync(DocumentUploadRequest request, CancellationToken ct = default);

    Task<(List<DocumentResponseDto> Items, long Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        string? storage = null,
        string? extension = null,
        string? articleFilter = null,
        CancellationToken ct = default);

    Task<List<DocumentResponseDto>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default);

    Task<DocumentResponseDto?> GetByIdAsync(string id, CancellationToken ct = default);

    Task<List<DocumentResponseDto>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default);

    Task<DocumentResponseDto> UpdateMetadataAsync(
        string id,
        string displayName,
        string? description,
        bool isPublic,
        CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);

    Task<bool> CanDownloadPublicAsync(string id, CancellationToken ct = default);

    Task<DocumentDownloadResult?> OpenDownloadAsync(
        string id,
        bool requirePublic,
        long? offset = null,
        long? length = null,
        CancellationToken ct = default);

    Task IncrementDownloadCountAsync(string id, CancellationToken ct = default);
}
