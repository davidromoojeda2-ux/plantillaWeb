namespace Aplication.DTOs;

/// <summary>
/// DTO con el estado de disponibilidad y prioridad de los proveedores de almacenamiento.
/// </summary>
public class StorageStatusDto
{
    /// <summary>Indica si el servicio S3/Cloudflare R2 está configurado y responde exitosamente.</summary>
    public bool IsS3Available { get; set; }

    /// <summary>Indica si el almacenamiento local está accesible para escritura.</summary>
    public bool IsLocalAvailable { get; set; }

    /// <summary>Nombre del bucket configurado en S3.</summary>
    public string S3Bucket { get; set; } = string.Empty;

    /// <summary>Dominio público o CDN de S3/Cloudflare R2.</summary>
    public string S3PublicDomain { get; set; } = string.Empty;

    /// <summary>Ruta base física del almacenamiento local.</summary>
    public string LocalStoragePath { get; set; } = string.Empty;
}
