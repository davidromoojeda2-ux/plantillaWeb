using Aplication.DTOs;
using Domine.Entities;

namespace Aplication.Mappers;

/// <summary>Convierte metadatos de documentos entre entidad y DTOs de aplicación.</summary>
public static class DocumentMapper
{
    public static DocumentResponseDto ToDto(DocumentMetadata document) => new()
    {
        Id = document.Id,
        OriginalFileName = document.OriginalFileName,
        DisplayName = document.DisplayName,
        Description = document.Description,
        Extension = document.Extension,
        ContentType = document.ContentType,
        SizeBytes = document.SizeBytes,
        Sha256 = document.Sha256,
        Storage = document.Storage,
        IsPublic = document.IsPublic,
        DownloadCount = document.DownloadCount,
        UploadedAt = document.UploadedAt
    };

    public static List<DocumentResponseDto> ToDtoList(IEnumerable<DocumentMetadata> documents) =>
        documents.Select(ToDto).ToList();

    /// <summary>Formatea un tamaño en bytes para mostrarlo en la interfaz.</summary>
    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024d:0.#} KB";
        return $"{bytes / (1024d * 1024d):0.#} MB";
    }
}
