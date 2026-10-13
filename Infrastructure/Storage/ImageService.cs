using Aplication.Interfaces;
using Domine.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Infrastructure.Storage;

/// <summary>
/// Servicio de procesamiento de imágenes con SixLabors.ImageSharp.
/// </summary>
public class ImageService
{
    private static readonly int[] TargetWidths = [400, 800, 1600];
    private readonly IImageStorage _storage;
    private readonly IImageMetadataRepository _metadataRepo;

    public ImageService(IImageStorage storage, IImageMetadataRepository metadataRepo)
    {
        _storage = storage;
        _metadataRepo = metadataRepo;
    }

    /// <summary>
    /// Procesa una imagen subida, genera variantes WebP y las almacena.
    /// </summary>
    /// <param name="originalFileName">Nombre original del archivo para trazabilidad.</param>
    /// <param name="inputStream">Stream con los bytes de la imagen original.</param>
    /// <param name="alt">Texto alternativo de accesibilidad.</param>
    /// <returns>El documento <see cref="ImageMetadata"/> persistido en MongoDB.</returns>
    public async Task<ImageMetadata> ProcessAndStoreAsync(
        string originalFileName, Stream inputStream, string alt, CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(inputStream, ct);

        // Corregir orientación EXIF
        image.Mutate(x => x.AutoOrient());

        int originalWidth = image.Width;
        int originalHeight = image.Height;

        // Limpiar metadatos EXIF para privacidad
        image.Metadata.ExifProfile = null;

        var variants = new List<ImageVariant>();
        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM");
        var baseName = Path.GetFileNameWithoutExtension(originalFileName)
                           .ToLowerInvariant()
                           .Replace(" ", "-");

        var encoder = new WebpEncoder { Quality = 80 };

        foreach (var width in TargetWidths)
        {
            // Solo generar variante si la imagen fuente es suficientemente ancha
            if (image.Width < width && width != TargetWidths[0]) continue;

            using var variant = image.Clone(ctx =>
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(Math.Min(width, image.Width), 0),
                    Mode = ResizeMode.Max
                });
            });

            var key = $"images/{dateFolder}/{baseName}-{width}.webp";
            using var ms = new MemoryStream();
            await variant.SaveAsync(ms, encoder, ct);
            var sizeBytes = ms.Length;
            ms.Position = 0;

            await _storage.UploadAsync(key, ms, "image/webp", ct);

            variants.Add(new ImageVariant
            {
                Width = width,
                Key = key,
                SizeBytes = sizeBytes,
                PublicUrl = _storage.GetPublicUrl(key)
            });
        }

        var metadata = new ImageMetadata
        {
            OriginalFileName = originalFileName,
            Alt = alt,
            OriginalWidth = originalWidth,
            OriginalHeight = originalHeight,
            Variants = variants,
            Storage = _storage is CloudflareR2Storage ? "r2" : "local"
        };

        await _metadataRepo.InsertAsync(metadata);
        return metadata;
    }

    /// <summary>
    /// Elimina todas las variantes del almacenamiento y el documento de metadatos.
    /// </summary>
    public async Task DeleteAsync(ImageMetadata metadata, CancellationToken ct = default)
    {
        foreach (var variant in metadata.Variants)
            await _storage.DeleteAsync(variant.Key, ct);

        await _metadataRepo.DeleteAsync(metadata.Id);
    }
}
