using Infrastructure.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Icc;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;
using SixLabors.ImageSharp.Metadata.Profiles.Xmp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageOptimization.Tests;

public class ImageMetadataCleanerTests
{
    [Fact]
    public void Strip_RemovesProfilesFromCanvasAndFrames()
    {
        using var image = new Image<Rgba32>(16, 16);

        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.XmpProfile = new XmpProfile();
        image.Metadata.IptcProfile = new IptcProfile();
        image.Metadata.IccProfile = new IccProfile();

        image.Frames.RootFrame.Metadata.ExifProfile = new ExifProfile();
        image.Frames.RootFrame.Metadata.XmpProfile = new XmpProfile();
        image.Frames.RootFrame.Metadata.IptcProfile = new IptcProfile();
        image.Frames.RootFrame.Metadata.IccProfile = new IccProfile();

        ImageMetadataCleaner.Strip(image);

        Assert.Null(image.Metadata.ExifProfile);
        Assert.Null(image.Metadata.XmpProfile);
        Assert.Null(image.Metadata.IptcProfile);
        Assert.Null(image.Metadata.IccProfile);

        Assert.Null(image.Frames.RootFrame.Metadata.ExifProfile);
        Assert.Null(image.Frames.RootFrame.Metadata.XmpProfile);
        Assert.Null(image.Frames.RootFrame.Metadata.IptcProfile);
        Assert.Null(image.Frames.RootFrame.Metadata.IccProfile);
    }

    [Fact]
    public void Strip_ThrowsForNullImage()
    {
        Assert.Throws<ArgumentNullException>(() => ImageMetadataCleaner.Strip(null!));
    }
}
