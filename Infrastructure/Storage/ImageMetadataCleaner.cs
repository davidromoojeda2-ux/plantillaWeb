using SixLabors.ImageSharp;

namespace Infrastructure.Storage;

/// <summary>
/// Elimina todos los perfiles de metadatos (EXIF, XMP, IPTC e ICC) de una imagen
/// y de cada uno de sus fotogramas antes de codificarla para el sitio público.
/// </summary>
public static class ImageMetadataCleaner
{
    public static void Strip(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);

        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.IccProfile = null;

        foreach (var frame in image.Frames)
        {
            frame.Metadata.ExifProfile = null;
            frame.Metadata.XmpProfile = null;
            frame.Metadata.IptcProfile = null;
            frame.Metadata.IccProfile = null;
        }
    }
}
