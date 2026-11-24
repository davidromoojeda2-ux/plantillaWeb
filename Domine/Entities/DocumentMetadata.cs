using Domine.Common;

namespace Domine.Entities;

/// <summary>
/// Metadatos de un documento descargable almacenado en S3/R2 o en el
/// almacenamiento local. El binario vive fuera de la base de datos; este documento
/// registra su ubicación, integridad y política de acceso.
/// </summary>
public class DocumentMetadata
{
    public string Id { get; set; } = EntityId.New();

    /// <summary>Nombre original del archivo subido, conservado para trazabilidad.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Nombre visible y saneado que se muestra en el CMS y en el portal.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Descripción opcional del documento.</summary>
    public string? Description { get; set; }

    /// <summary>Extensión normalizada, incluyendo el punto (por ejemplo, ".pdf").</summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>MIME type resuelto en servidor a partir de la extensión validada.</summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>Tamaño del binario en bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Hash SHA-256 del contenido, usado para integridad y ETag.</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>Proveedor efectivo de almacenamiento: "s3", "r2" o "local".</summary>
    public string Storage { get; set; } = "local";

    /// <summary>Clave generada dentro del almacenamiento, por ejemplo "documents/2026/11/....pdf".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Si es true, el documento puede descargarse sin depender de un artículo publicado.</summary>
    public bool IsPublic { get; set; }

    /// <summary>Número de descargas públicas completadas correctamente.</summary>
    public long DownloadCount { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
