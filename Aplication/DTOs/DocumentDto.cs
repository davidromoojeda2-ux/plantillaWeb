using System.ComponentModel.DataAnnotations;

namespace Aplication.DTOs;

/// <summary>
/// Solicitud de subida de un documento. El stream se valida y se almacena
/// antes de que el servicio devuelva los metadatos persistidos.
/// </summary>
public sealed class DocumentUploadRequest
{
    public required Stream Content { get; init; }

    public required string OriginalFileName { get; init; }

    [StringLength(200, ErrorMessage = "El nombre visible no puede superar los 200 caracteres.")]
    public string? DisplayName { get; init; }

    [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres.")]
    public string? Description { get; init; }

    public bool IsPublic { get; init; }
}

/// <summary>DTO de respuesta con los metadatos de un documento.</summary>
public class DocumentResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Extension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string Storage { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public long DownloadCount { get; set; }
    public DateTime UploadedAt { get; set; }

    /// <summary>Artículo que usa el documento, si existe. Se completa en la capa de presentación.</summary>
    public string? ArticleId { get; set; }

    public string? ArticleTitle { get; set; }
}

/// <summary>Resultado de abrir un documento para descarga o streaming por rango.</summary>
public sealed record DocumentDownloadResult(
    Stream Stream,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    DateTime UploadedAt,
    bool IsPublic);
