using Aplication.DTOs.Images;
using Aplication.Exceptions;
using Aplication.Interfaces;
using ImageMagick;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Images;

/// <summary>
/// Adaptador de ImageMagick/Magick.NET para <see cref="IImageProcessor"/>.
/// Mantiene el resto de la aplicación libre de tipos de ImageMagick y centraliza
/// las políticas de formatos, límites de recursos y codificación.
/// </summary>
public sealed class MagickImageProcessor : IImageProcessor
{
    private static readonly HashSet<MagickFormat> AllowedFormats =
    [
        MagickFormat.Jpeg,
        MagickFormat.Png,
        MagickFormat.WebP,
        MagickFormat.Gif,
        MagickFormat.Bmp,
        MagickFormat.Tiff,
        MagickFormat.Tga,
        MagickFormat.Pbm,
        MagickFormat.Pnm,
        MagickFormat.Qoi,
        MagickFormat.Avif,
        MagickFormat.Heic
    ];

    static MagickImageProcessor()
    {
        // Límites para reducir el impacto de archivos manipulados (decompression bombs).
        ResourceLimits.Memory = 512UL * 1024 * 1024;
        ResourceLimits.Width = 12000;
        ResourceLimits.Height = 12000;
        ResourceLimits.Thread = 2;
    }

    private readonly ILogger<MagickImageProcessor> _logger;

    public MagickImageProcessor(ILogger<MagickImageProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessedImage> ProcessAsync(
        Stream input,
        ImageProcessingOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);

        if (options.TargetWidths.Count == 0)
            throw new ArgumentException(
                "Debe indicarse al menos un ancho de destino.", nameof(options));

        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var format = ReadFormat(buffer);
        if (!AllowedFormats.Contains(format))
        {
            throw new UnsupportedImageFormatException(
                "El archivo no es un formato de imagen soportado. " +
                "Usa JPEG, PNG, WebP, GIF, BMP, TIFF, AVIF o HEIC.");
        }

        buffer.Position = 0;

        try
        {
            using var source = new MagickImageCollection();
            await source.ReadAsync(buffer, ct);

            if (source.Count == 0)
                throw new InvalidImageContentException(
                    "La imagen no contiene ningún fotograma válido.");

            foreach (var frame in source)
            {
                frame.AutoOrient();
                frame.Strip();
            }

            var originalWidth = (int)source[0].Width;
            var originalHeight = (int)source[0].Height;
            var variants = new List<ProcessedImageVariant>(options.TargetWidths.Count);

            foreach (var requestedWidth in options.TargetWidths)
            {
                if (requestedWidth <= 0) continue;
                if (originalWidth < requestedWidth &&
                    requestedWidth != options.TargetWidths[0])
                {
                    continue;
                }

                var resizeWidth = (uint)Math.Min(requestedWidth, originalWidth);
                var quality = (uint)options.QualityFor(requestedWidth);
                var outputFormat = MapFormat(options.OutputFormat);

                using var variant = new MagickImageCollection();

                foreach (var frame in source)
                {
                    var clone = frame.Clone();
                    clone.Resize(new MagickGeometry(resizeWidth, 0));
                    clone.Strip();
                    clone.Quality = quality;
                    variant.Add(clone);
                }

                using var output = new MemoryStream();
                await variant.WriteAsync(output, outputFormat, ct);

                variants.Add(new ProcessedImageVariant(
                    requestedWidth,
                    (int)variant[0].Width,
                    (int)variant[0].Height,
                    output.ToArray(),
                    ContentType(options.OutputFormat)));
            }

            return new ProcessedImage(originalWidth, originalHeight, variants);
        }
        catch (MagickException ex)
        {
            _logger.LogWarning(ex, "No se pudo decodificar o procesar una imagen.");
            throw new InvalidImageContentException(
                "La imagen está dañada o su contenido no es válido.", ex);
        }
    }

    private static MagickFormat ReadFormat(Stream stream)
    {
        try
        {
            var info = new MagickImageInfo(stream);
            return info.Format;
        }
        catch (MagickException ex)
        {
            throw new UnsupportedImageFormatException(
                "El archivo no es una imagen reconocida. " +
                $"Detalle: {ex.Message}");
        }
    }

    private static MagickFormat MapFormat(ImageOutputFormat format) => format switch
    {
        ImageOutputFormat.Avif => MagickFormat.Avif,
        _ => MagickFormat.WebP
    };

    private static string ContentType(ImageOutputFormat format) => format switch
    {
        ImageOutputFormat.Avif => "image/avif",
        _ => "image/webp"
    };
}
