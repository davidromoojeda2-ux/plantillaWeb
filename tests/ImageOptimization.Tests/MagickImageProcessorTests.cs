using Aplication.DTOs.Images;
using Aplication.Exceptions;
using ImageMagick;
using Infrastructure.Images;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImageOptimization.Tests;

public class MagickImageProcessorTests
{
    private static readonly int[] Widths = [400, 800, 1600];

    [Fact]
    public async Task Genera_variantes_webp_y_elimina_metadatos()
    {
        using var source = new MagickImage(MagickColors.CornflowerBlue, 2000, 1200);
        source.Comment = "dato privado";

        using var input = new MemoryStream();
        source.Write(input, MagickFormat.Jpeg);
        input.Position = 0;

        var result = await CreateProcessor().ProcessAsync(input, Options());

        Assert.Equal(2000, result.OriginalWidth);
        Assert.Equal(1200, result.OriginalHeight);
        Assert.Equal(3, result.Variants.Count);
        Assert.Equal(
            new[] { 400, 800, 1600 },
            result.Variants.Select(variant => variant.RequestedWidth).ToArray());

        foreach (var variant in result.Variants)
        {
            Assert.True(variant.Bytes.Length > 0);

            using var output = new MagickImage(variant.Bytes);
            Assert.Equal(MagickFormat.WebP, output.Format);
            Assert.Equal(variant.Width, (int)output.Width);
            Assert.Equal(variant.Height, (int)output.Height);
            Assert.Empty(output.ProfileNames);
            Assert.Null(output.Comment);
        }
    }

    [Fact]
    public async Task Conserva_los_fotogramas_de_una_imagen_animada()
    {
        using var frames = new MagickImageCollection();
        frames.Add(new MagickImage(MagickColors.Red, 1200, 800));
        frames.Add(new MagickImage(MagickColors.Blue, 1200, 800));

        using var input = new MemoryStream();
        frames.Write(input, MagickFormat.Gif);
        input.Position = 0;

        var result = await CreateProcessor().ProcessAsync(input, Options());
        var variant = Assert.Single(
            result.Variants, item => item.RequestedWidth == 800);

        using var output = new MagickImageCollection(variant.Bytes);
        Assert.Equal(2, output.Count);
    }

    [Fact]
    public async Task Rechaza_formatos_no_permitidos()
    {
        using var input = new MemoryStream(
            System.Text.Encoding.UTF8.GetBytes("esto no es una imagen"));

        await Assert.ThrowsAsync<UnsupportedImageFormatException>(() =>
            CreateProcessor().ProcessAsync(input, Options()));
    }

    [Fact]
    public async Task No_genera_variantes_mayores_que_la_imagen()
    {
        using var source = new MagickImage(MagickColors.Green, 900, 600);
        using var input = new MemoryStream();
        source.Write(input, MagickFormat.Png);
        input.Position = 0;

        var result = await CreateProcessor().ProcessAsync(input, Options());

        Assert.Equal(
            new[] { 400, 800 },
            result.Variants.Select(variant => variant.RequestedWidth).ToArray());
    }

    [Fact]
    public void El_build_nativo_soporta_avif_y_heic()
    {
        Assert.True(MagickFormatInfo.Create(MagickFormat.Avif)?.SupportsWriting);
        Assert.True(MagickFormatInfo.Create(MagickFormat.Heic)?.SupportsReading);
    }

    [Fact]
    public async Task Puede_generar_avif_cuando_se_solicita()
    {
        using var source = new MagickImage(MagickColors.Orange, 600, 400);
        using var input = new MemoryStream();
        source.Write(input, MagickFormat.Png);
        input.Position = 0;

        var options = Options() with { OutputFormat = ImageOutputFormat.Avif };
        var result = await CreateProcessor().ProcessAsync(input, options);

        using var output = new MagickImage(result.Variants[0].Bytes);
        Assert.Equal(MagickFormat.Avif, output.Format);
    }

    private static MagickImageProcessor CreateProcessor() =>
        new(NullLogger<MagickImageProcessor>.Instance);

    private static ImageProcessingOptions Options() =>
        new(
            Widths,
            new Dictionary<int, int>
            {
                [400] = 80,
                [800] = 80,
                [1600] = 80
            },
            DefaultQuality: 80,
            OutputFormat: ImageOutputFormat.WebP);
}
