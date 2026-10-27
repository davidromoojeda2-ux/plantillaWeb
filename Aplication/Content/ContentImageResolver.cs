using Aplication.DTOs.ContentBlocks;

namespace Aplication.Content;

/// <summary>
/// Utilidades de consulta sobre el cuerpo estructurado que no dependen de la
/// persistencia ni del renderizado.
/// </summary>
public static class ContentImageResolver
{
    /// <summary>Devuelve el primer bloque de imagen con URL o referencia de biblioteca.</summary>
    public static ContentImageBlockDto? FindFirstImage(IEnumerable<ContentBlockDto>? blocks) =>
        blocks?.OfType<ContentImageBlockDto>()
            .FirstOrDefault(image =>
                !string.IsNullOrWhiteSpace(image.ImageId) ||
                !string.IsNullOrWhiteSpace(image.Url));

    public static bool HasImage(IEnumerable<ContentBlockDto>? blocks) =>
        FindFirstImage(blocks) is not null;
}
