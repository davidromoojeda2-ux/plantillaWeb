using Aplication.DTOs.Images;

namespace Aplication.Interfaces;

/// <summary>
/// Estrategia de procesamiento de imágenes. La implementación concreta vive en
/// Infrastructure; el resto de la aplicación depende solo de esta abstracción.
/// </summary>
public interface IImageProcessor
{
    Task<ProcessedImage> ProcessAsync(
        Stream input,
        ImageProcessingOptions options,
        CancellationToken ct = default);
}
