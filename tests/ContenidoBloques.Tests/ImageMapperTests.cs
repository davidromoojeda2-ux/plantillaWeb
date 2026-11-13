using Aplication.DTOs;
using Aplication.Mappers;

namespace ContenidoBloques.Tests;

public class ImageMapperTests
{
    private static ImageMetadataResponseDto BuildImage(params ImageVariantDto[] variants) => new()
    {
        Id = "img-1",
        OriginalFileName = "foto.jpg",
        Alt = "Foto de prueba",
        OriginalWidth = 1600,
        OriginalHeight = 900,
        Variants = [.. variants]
    };

    [Fact]
    public void GetSrcSet_DevuelveNull_CuandoNoHayImagen()
    {
        Assert.Null(ImageMapper.GetSrcSet(null));
    }

    [Fact]
    public void GetSrcSet_OrdenaVariantesYOmiteUrlsVacias()
    {
        var image = BuildImage(
            new ImageVariantDto { Width = 1600, PublicUrl = "https://cdn.example.com/foto-1600.webp" },
            new ImageVariantDto { Width = 400, PublicUrl = "https://cdn.example.com/foto-400.webp" },
            new ImageVariantDto { Width = 800, PublicUrl = null });

        var srcSet = ImageMapper.GetSrcSet(image);

        Assert.Equal(
            "https://cdn.example.com/foto-400.webp 400w, https://cdn.example.com/foto-1600.webp 1600w",
            srcSet);
    }

    [Fact]
    public void GetSrcSet_DevuelveNull_CuandoNingunaVarianteTieneUrl()
    {
        var image = BuildImage(
            new ImageVariantDto { Width = 400, PublicUrl = " " },
            new ImageVariantDto { Width = 800, PublicUrl = null });

        Assert.Null(ImageMapper.GetSrcSet(image));
    }

    [Fact]
    public void GetSrcSetForUrl_DevuelveSrcSet_CuandoNoHayUrlPreferida()
    {
        var image = BuildImage(
            new ImageVariantDto { Width = 400, PublicUrl = "https://cdn.example.com/foto-400.webp" },
            new ImageVariantDto { Width = 800, PublicUrl = "https://cdn.example.com/foto-800.webp" });

        Assert.Equal(
            "https://cdn.example.com/foto-400.webp 400w, https://cdn.example.com/foto-800.webp 800w",
            ImageMapper.GetSrcSetForUrl(image, null));
    }

    [Fact]
    public void GetSrcSetForUrl_DevuelveNull_CuandoLaUrlEsExterna()
    {
        var image = BuildImage(
            new ImageVariantDto { Width = 400, PublicUrl = "https://cdn.example.com/foto-400.webp" },
            new ImageVariantDto { Width = 800, PublicUrl = "https://cdn.example.com/foto-800.webp" });

        Assert.Null(ImageMapper.GetSrcSetForUrl(image, "https://images.unsplash.com/foto-externa.jpg"));
    }

    [Fact]
    public void GetSrcSetForUrl_DevuelveSrcSet_CuandoLaUrlEsUnaVarianteRegistrada()
    {
        var image = BuildImage(
            new ImageVariantDto { Width = 400, PublicUrl = "https://cdn.example.com/foto-400.webp" },
            new ImageVariantDto { Width = 800, PublicUrl = "https://cdn.example.com/foto-800.webp" },
            new ImageVariantDto { Width = 1600, PublicUrl = "https://cdn.example.com/foto-1600.webp" });

        var srcSet = ImageMapper.GetSrcSetForUrl(image, "https://cdn.example.com/foto-800.webp");

        Assert.Equal(
            "https://cdn.example.com/foto-400.webp 400w, https://cdn.example.com/foto-800.webp 800w, https://cdn.example.com/foto-1600.webp 1600w",
            srcSet);
    }

    [Fact]
    public void GetUrlForWidth_SeleccionaLaVarianteMasCercana()
    {
        var image = BuildImage(
            new ImageVariantDto { Width = 400, PublicUrl = "https://cdn.example.com/foto-400.webp" },
            new ImageVariantDto { Width = 800, PublicUrl = "https://cdn.example.com/foto-800.webp" },
            new ImageVariantDto { Width = 1600, PublicUrl = "https://cdn.example.com/foto-1600.webp" });

        Assert.Equal("https://cdn.example.com/foto-800.webp", ImageMapper.GetUrlForWidth(image, 700));
        Assert.Equal("https://cdn.example.com/foto-1600.webp", ImageMapper.GetUrlForWidth(image, 1400));
    }
}
