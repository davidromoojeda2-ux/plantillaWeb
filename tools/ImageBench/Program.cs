using System.Diagnostics;
using System.Globalization;
using System.Text;
using ImageMagick;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

using SharpImage = SixLabors.ImageSharp.Image;
using SharpSize = SixLabors.ImageSharp.Size;
using SharpResizeOptions = SixLabors.ImageSharp.Processing.ResizeOptions;
using SharpResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;

namespace ImageBench;

/// <summary>
/// POC de comparación entre el pipeline actual con ImageSharp y una alternativa
/// con Magick.NET. Mide tamaño de salida y tiempo para WebP y AVIF.
/// </summary>
internal static class Program
{
    private static readonly int[] Widths = [400, 800, 1600];
    private const int WebpQuality = 80;
    private const int AvifQuality = 60;

    private static readonly string[] ImageExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".avif", ".tif", ".tiff", ".heic", ".heif"];

    private sealed record InputImage(
        string Path, string Label, long Bytes, int Width, int Height, string Format,
        bool HasAlpha);

    private sealed record VariantResult(
        string Library, string OutputFormat, int Width, int Height,
        long Bytes, double Milliseconds, bool MetadataStripped, string? Error,
        double? Ssim = null, double? Psnr = null);

    private sealed record InputResult(
        string Label, string Format, long SourceBytes, int SourceWidth, int SourceHeight,
        List<VariantResult> Variants);

    private static async Task<int> Main(string[] args)
    {
        var projectDir = FindProjectDirectory();
        var repoRoot = FindRepositoryRoot(projectDir);
        var sampleDir = Path.Combine(projectDir, "samples");
        Directory.CreateDirectory(sampleDir);

        var inputDir = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(repoRoot, "paginaWeb", "wwwroot", "uploads", "images");

        var outputDir = args.Length > 1
            ? Path.GetFullPath(args[1])
            : Path.Combine(projectDir, "out");
        Directory.CreateDirectory(outputDir);

        var inputs = LoadInputs(inputDir, sampleDir);
        if (inputs.Count == 0)
        {
            Write("No se encontraron imágenes reales; se generará una muestra sintética.");
            GenerateSyntheticSamples(sampleDir);
            inputs = LoadInputs(sampleDir, sampleDir);
        }

        if (inputs.Count == 0)
        {
            Write("No hay imágenes para procesar.");
            return 1;
        }

        Write($"Entrada: {inputDir}");
        Write($"Salida:  {outputDir}");
        Write($"Imágenes: {inputs.Count}");
        Write("");

        var results = new List<InputResult>();
        foreach (var input in inputs)
        {
            Write($"=== {input.Label} ({input.Format}, {input.Width}x{input.Height}, alpha: {input.HasAlpha}, {Bytes(input.Bytes)}) ===");

            var variants = new List<VariantResult>();
            variants.AddRange(await RunImageSharpAsync(input, lossy: false, "ImageSharp"));
            variants.AddRange(await RunImageSharpAsync(input, lossy: true, "ImageSharp-L"));
            variants.AddRange(RunMagick(input));

            foreach (var variant in variants)
            {
                var status = variant.Error is null
                    ? $"{variant.Width}x{variant.Height} {Bytes(variant.Bytes),10} " +
                      $"{variant.Milliseconds,8:F0} ms" +
                      (variant.Ssim is null ? string.Empty : $"  SSIM-diff {variant.Ssim:F4}")
                    : $"ERROR: {variant.Error}";

                Write($"  {variant.Library,-12} {variant.OutputFormat,-5} " +
                      $"{(variant.Width > 0 ? variant.Width + "px" : "-"),-6} {status}");
            }

            results.Add(new InputResult(
                input.Label, input.Format, input.Bytes, input.Width, input.Height, variants));
            Write("");
        }

        WriteSummary(results);
        WriteCsv(Path.Combine(outputDir, "results.csv"), results);
        Write($"CSV: {Path.Combine(outputDir, "results.csv")}");

        return 0;
    }

    private static async Task<List<VariantResult>> RunImageSharpAsync(
        InputImage input, bool lossy, string label)
    {
        var results = new List<VariantResult>();
        var total = Stopwatch.StartNew();

        try
        {
            using var image = await SharpImage.LoadAsync(input.Path);
            image.Mutate(context => context.AutoOrient());
            StripMetadata(image);

            foreach (var width in Widths)
            {
                if (image.Width < width && width != Widths[0]) continue;

                var targetWidth = Math.Min(width, image.Width);
                var watch = Stopwatch.StartNew();

                using var variant = image.Clone(context => context.Resize(new SharpResizeOptions
                {
                    Size = new SharpSize(targetWidth, 0),
                    Mode = SharpResizeMode.Max
                }));

                var encoder = lossy
                    ? new WebpEncoder
                    {
                        Quality = WebpQuality,
                        FileFormat = WebpFileFormatType.Lossy
                    }
                    : new WebpEncoder { Quality = WebpQuality };

                using var output = new MemoryStream();
                variant.Save(output, encoder);
                watch.Stop();

                var (ssim, psnr) = QualityMetrics(output.ToArray(), input.Path, targetWidth);

                results.Add(new VariantResult(
                    label, "webp", variant.Width, variant.Height,
                    output.Length, watch.Elapsed.TotalMilliseconds, true, null,
                    ssim, psnr));
            }
        }
        catch (Exception ex)
        {
            results.Add(new VariantResult(
                label, "webp", 0, 0, 0, total.Elapsed.TotalMilliseconds,
                false, $"{ex.GetType().Name}: {ex.Message}"));
        }

        return results;
    }

    private static List<VariantResult> RunMagick(InputImage input)
    {
        var results = new List<VariantResult>();

        try
        {
            using var image = new MagickImage(input.Path);
            image.AutoOrient();
            image.Strip();

            var metadataStripped = !image.ProfileNames.Any();

            foreach (var width in Widths)
            {
                if (image.Width < width && width != Widths[0]) continue;

                var targetWidth = (uint)Math.Min(width, image.Width);

                var webpWatch = Stopwatch.StartNew();
                using (var variant = image.Clone())
                {
                    variant.Resize(new MagickGeometry(targetWidth, 0));
                    variant.Format = MagickFormat.WebP;
                    variant.Quality = WebpQuality;

                    using var output = new MemoryStream();
                    variant.Write(output);
                    webpWatch.Stop();

                    var (ssim, psnr) = QualityMetrics(
                        output.ToArray(), input.Path, (int)targetWidth);

                    results.Add(new VariantResult(
                        "Magick.NET", "webp", (int)variant.Width, (int)variant.Height,
                        output.Length, webpWatch.Elapsed.TotalMilliseconds,
                        metadataStripped, null, ssim, psnr));
                }

                var avifWatch = Stopwatch.StartNew();
                using (var variant = image.Clone())
                {
                    variant.Resize(new MagickGeometry(targetWidth, 0));
                    variant.Format = MagickFormat.Avif;
                    variant.Quality = AvifQuality;
                    variant.Settings.SetDefine("heic:speed", "6");

                    using var output = new MemoryStream();
                    variant.Write(output);
                    avifWatch.Stop();

                    var (ssim, psnr) = QualityMetrics(
                        output.ToArray(), input.Path, (int)targetWidth);

                    results.Add(new VariantResult(
                        "Magick.NET", "avif", (int)variant.Width, (int)variant.Height,
                        output.Length, avifWatch.Elapsed.TotalMilliseconds,
                        metadataStripped, null, ssim, psnr));
                }
            }
        }
        catch (Exception ex)
        {
            results.Add(new VariantResult(
                "Magick.NET", "webp/avif", 0, 0, 0, 0,
                false, $"{ex.GetType().Name}: {ex.Message}"));
        }

        return results;
    }

    private static (double? Ssim, double? Psnr) QualityMetrics(
        byte[] encoded, string sourcePath, int targetWidth)
    {
        if (targetWidth != 800) return (null, null);

        try
        {
            using var reference = new MagickImage(sourcePath);
            reference.AutoOrient();
            reference.Resize(new MagickGeometry((uint)targetWidth, 0));

            using var actual = new MagickImage(encoded);
            var ssim = actual.Compare(reference, ErrorMetric.StructuralSimilarity);
            var psnr = actual.Compare(reference, ErrorMetric.PeakSignalToNoiseRatio);
            return (ssim, psnr);
        }
        catch
        {
            return (null, null);
        }
    }

    private static void StripMetadata(SharpImage image)
    {
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

    private static List<InputImage> LoadInputs(string inputDir, string sampleDir)
    {
        var candidates = new List<string>();

        if (File.Exists(inputDir) && HasImageExtension(inputDir))
        {
            candidates.Add(inputDir);
        }
        else if (Directory.Exists(inputDir))
        {
            candidates.AddRange(Directory
                .EnumerateFiles(inputDir, "*", SearchOption.AllDirectories)
                .Where(HasImageExtension)
                .OrderByDescending(path => new FileInfo(path).Length)
                .Take(5));
        }

        // Genera variantes de formato a partir de la primera imagen real para
        // comprobar la cobertura de entrada (JPEG, PNG, TIFF, AVIF, orientación EXIF).
        if (candidates.Count > 0)
            CreateFormatSamples(candidates[0], sampleDir);

        if (Directory.Exists(sampleDir))
        {
            candidates.AddRange(Directory
                .EnumerateFiles(sampleDir, "*", SearchOption.AllDirectories)
                .Where(HasImageExtension)
                .Where(path => !candidates.Contains(path, StringComparer.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
        }

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(ToInputImage)
            .Where(input => input is not null)
            .Cast<InputImage>()
            .ToList();
    }

    private static InputImage? ToInputImage(string path)
    {
        try
        {
            using var image = new MagickImage(path);
            return new InputImage(
                path, Path.GetFileName(path), new FileInfo(path).Length,
                (int)image.Width, (int)image.Height, image.Format.ToString(),
                image.HasAlpha);
        }
        catch
        {
            return null;
        }
    }

    private static void CreateFormatSamples(string sourcePath, string sampleDir)
    {
        var marker = Path.Combine(sampleDir, ".generated");
        if (File.Exists(marker)) return;

        try
        {
            using var source = new MagickImage(sourcePath);
            source.Strip();

            WriteSample(source, Path.Combine(sampleDir, "sample.jpg"), MagickFormat.Jpeg, 92);
            WriteSample(source, Path.Combine(sampleDir, "sample.png"), MagickFormat.Png, 0);
            WriteSample(source, Path.Combine(sampleDir, "sample.tiff"), MagickFormat.Tiff, 0);

            if (MagickFormatInfo.Create(MagickFormat.Avif)?.SupportsWriting == true)
                WriteSample(source, Path.Combine(sampleDir, "sample.avif"), MagickFormat.Avif, 60);

            File.WriteAllText(marker, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            Write($"No se pudieron generar las muestras de formato: {ex.Message}");
        }
    }

    private static void WriteSample(MagickImage source, string path, MagickFormat format, int quality)
    {
        using var clone = source.Clone();
        clone.Format = format;
        if (quality > 0) clone.Quality = (uint)quality;
        clone.Write(path);
    }

    private static void GenerateSyntheticSamples(string sampleDir)
    {
        if (File.Exists(Path.Combine(sampleDir, "synthetic.jpg"))) return;

        using var image = new MagickImage(MagickColors.CornflowerBlue, 2400, 1600);
        image.Quality = 92;
        image.Write(Path.Combine(sampleDir, "synthetic.jpg"), MagickFormat.Jpeg);
        image.Write(Path.Combine(sampleDir, "synthetic.png"), MagickFormat.Png);
    }

    private static void WriteSummary(List<InputResult> results)
    {
        Write("=== Resumen ===");

        var groups = results
            .SelectMany(result => result.Variants)
            .Where(variant => variant.Error is null)
            .GroupBy(variant => (variant.Library, variant.OutputFormat))
            .OrderBy(group => group.Key.Library)
            .ThenBy(group => group.Key.OutputFormat);

        foreach (var group in groups)
        {
            var totalBytes = group.Sum(variant => variant.Bytes);
            var totalMs = group.Sum(variant => variant.Milliseconds);
            Write($"{group.Key.Library,-12} {group.Key.OutputFormat,-5} " +
                  $"{group.Count(),3} variantes  {Bytes(totalBytes),10}  {totalMs,9:F0} ms");
        }

        var webp = results
            .SelectMany(result => result.Variants)
            .Where(variant => variant.Error is null && variant.OutputFormat == "webp")
            .GroupBy(variant => variant.Library)
            .ToDictionary(group => group.Key, group => group.ToList());

        if (webp.Count > 0)
        {
            Write("");

            Compare("Magick.NET", "ImageSharp");
            Compare("Magick.NET", "ImageSharp-L");
            Compare("ImageSharp-L", "ImageSharp");

            void Compare(string currentLabel, string baselineLabel)
            {
                if (!webp.TryGetValue(currentLabel, out var current) ||
                    !webp.TryGetValue(baselineLabel, out var baseline) ||
                    current.Count == 0 || baseline.Count == 0)
                    return;

                var currentBytes = current.Sum(variant => variant.Bytes);
                var baselineBytes = baseline.Sum(variant => variant.Bytes);
                var currentMs = current.Sum(variant => variant.Milliseconds);
                var baselineMs = baseline.Sum(variant => variant.Milliseconds);

                Write($"WebP {currentLabel} vs {baselineLabel}: " +
                      $"{Delta(currentBytes, baselineBytes)} en peso, " +
                      $"{Delta((long)currentMs, (long)baselineMs)} en tiempo.");
            }
        }

        var fairPairs = results
            .SelectMany(result => result.Variants
                .Where(variant => variant.Error is null && variant.OutputFormat == "webp")
                .Select(variant => new
                {
                    result.Label,
                    variant.Width,
                    variant.Library,
                    variant.Bytes,
                    variant.Milliseconds
                }))
            .GroupBy(item => (item.Label, item.Width))
            .Where(group =>
                group.Any(item => item.Library == "ImageSharp-L") &&
                group.Any(item => item.Library == "Magick.NET"))
            .ToList();

        if (fairPairs.Count > 0)
        {
            long sharpBytes = 0;
            long magickBytes = 0;
            double sharpMs = 0;
            double magickMs = 0;

            foreach (var group in fairPairs)
            {
                var sharp = group.First(item => item.Library == "ImageSharp-L");
                var magick = group.First(item => item.Library == "Magick.NET");
                sharpBytes += sharp.Bytes;
                magickBytes += magick.Bytes;
                sharpMs += sharp.Milliseconds;
                magickMs += magick.Milliseconds;
            }

            Write("");
            Write($"Comparación justa WebP ({fairPairs.Count} variantes, mismos inputs y anchos):");
            Write($"  Magick.NET {Bytes(magickBytes)} / {magickMs:F0} ms vs " +
                  $"ImageSharp-L {Bytes(sharpBytes)} / {sharpMs:F0} ms  →  " +
                  $"{Delta(magickBytes, sharpBytes)} peso, " +
                  $"{Delta((long)magickMs, (long)sharpMs)} tiempo.");
        }

        Write("");
    }

    private static string Delta(long current, long baseline)
    {
        if (baseline == 0) return "sin base";
        var percent = (current - baseline) * 100.0 / baseline;
        return $"{percent:+0.0;-0.0;0.0}%";
    }

    private static void WriteCsv(string path, List<InputResult> results)
    {
        var builder = new StringBuilder();
        builder.AppendLine("input,format,source_bytes,source_width,source_height,library,output_format,width,height,bytes,milliseconds,metadata_stripped,error,ssim,psnr");

        foreach (var result in results)
        {
            foreach (var variant in result.Variants)
            {
                builder.AppendLine(string.Join(',',
                    Csv(result.Label), Csv(result.Format), result.SourceBytes,
                    result.SourceWidth, result.SourceHeight,
                    Csv(variant.Library), Csv(variant.OutputFormat),
                    variant.Width, variant.Height, variant.Bytes,
                    variant.Milliseconds.ToString("F2", CultureInfo.InvariantCulture),
                    variant.MetadataStripped, Csv(variant.Error),
                    variant.Ssim?.ToString("F4", CultureInfo.InvariantCulture) ?? "",
                    variant.Psnr?.ToString("F2", CultureInfo.InvariantCulture) ?? ""));
            }
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    private static string Csv(string? value) =>
        "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

    private static string Bytes(long bytes) => bytes switch
    {
        >= 1_000_000 => $"{bytes / 1_000_000.0:F2} MB",
        >= 1_000 => $"{bytes / 1_000.0:F1} KB",
        _ => $"{bytes} B"
    };

    private static bool HasImageExtension(string path) =>
        ImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static string FindProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ImageBench.csproj")))
                return directory.FullName;
            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string FindRepositoryRoot(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "paginaWeb.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }

        return start;
    }

    private static void Write(string message) => Console.WriteLine(message);
}
