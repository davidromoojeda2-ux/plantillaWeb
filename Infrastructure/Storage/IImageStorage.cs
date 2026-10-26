namespace Infrastructure.Storage;

/// <summary>
/// Abstracción de almacenamiento de imágenes binarias.
/// Implementaciones: CloudflareR2Storage (producción) y LocalStorage (desarrollo).
/// </summary>
public interface IImageStorage
{
    /// <summary>
    /// Sube un stream de bytes al proveedor de almacenamiento.
    /// </summary>
    /// <param name="key">Clave del objeto, e.g. "images/2025/01/photo-800.webp".</param>
    /// <param name="stream">Stream con los bytes del archivo.</param>
    /// <param name="contentType">MIME type, e.g. "image/webp".</param>
    Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct = default);

    /// <summary>Elimina un objeto del almacenamiento por clave.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>Construye la URL pública de acceso CDN para una clave dada.</summary>
    string GetPublicUrl(string key);

    /// <summary>Verifica la disponibilidad y conectividad del servicio de almacenamiento.</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>Identificador del tipo de almacenamiento (e.g. "s3", "r2", "local").</summary>
    string StorageType { get; }
}

