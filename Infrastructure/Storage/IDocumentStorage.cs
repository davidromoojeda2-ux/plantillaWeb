namespace Infrastructure.Storage;

/// <summary>
/// Almacenamiento de documentos con lectura por streaming. Los documentos se
/// guardan de forma privada; la aplicación es la única que decide cuándo y a
/// quién entregarlos.
/// </summary>
public interface IDocumentStorage
{
    Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Abre un documento para lectura. Si se indica <paramref name="offset"/> se
    /// debe indicar también <paramref name="length"/>; el resultado contiene solo
    /// ese rango para soportar descargas reanudables.
    /// </summary>
    Task<Stream?> OpenReadAsync(string key, long? offset = null, long? length = null, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);

    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    string StorageType { get; }
}
