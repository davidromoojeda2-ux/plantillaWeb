using System.Globalization;
using System.Xml.Linq;

namespace paginaWeb.Seo;

/// <summary>Entrada del índice de sitemaps.</summary>
public sealed record SitemapIndexEntry(string Path, DateTime? LastModified);

/// <summary>URL incluida en un sitemap, con sus imágenes asociadas.</summary>
public sealed record SitemapUrlEntry(
    string Path,
    DateTime? LastModified,
    IReadOnlyCollection<SitemapImage>? Images = null);

/// <summary>Imagen declarada mediante la extensión de sitemaps de imágenes de Google.</summary>
public sealed record SitemapImage(string Url, string? Caption = null, string? Title = null);

/// <summary>
/// Construye los documentos XML de sitemaps (índice y urlset) sin depender de MVC.
/// </summary>
public static class SitemapDocumentBuilder
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace ImageNs = "http://www.google.com/schemas/sitemap-image/1.1";

    public static XDocument BuildIndex(string baseUrl, IEnumerable<SitemapIndexEntry> entries)
    {
        var sitemaps = entries.Select(entry =>
        {
            var element = new XElement(
                SitemapNs + "sitemap",
                new XElement(SitemapNs + "loc", ToAbsoluteUrl(baseUrl, entry.Path)));

            if (entry.LastModified is not null)
                element.Add(new XElement(SitemapNs + "lastmod", FormatLastModified(entry.LastModified.Value)));

            return element;
        });

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(SitemapNs + "sitemapindex", sitemaps));
    }

    public static XDocument BuildUrlSet(string baseUrl, IEnumerable<SitemapUrlEntry> entries)
    {
        var urls = entries.Select(entry =>
        {
            var element = new XElement(
                SitemapNs + "url",
                new XElement(SitemapNs + "loc", ToAbsoluteUrl(baseUrl, entry.Path)));

            if (entry.LastModified is not null)
                element.Add(new XElement(SitemapNs + "lastmod", FormatLastModified(entry.LastModified.Value)));

            foreach (var image in entry.Images ?? [])
            {
                var imageElement = new XElement(
                    ImageNs + "image",
                    new XElement(ImageNs + "loc", ToAbsoluteUrl(baseUrl, image.Url)));

                if (!string.IsNullOrWhiteSpace(image.Caption))
                    imageElement.Add(new XElement(ImageNs + "caption", image.Caption));

                if (!string.IsNullOrWhiteSpace(image.Title))
                    imageElement.Add(new XElement(ImageNs + "title", image.Title));

                element.Add(imageElement);
            }

            return element;
        });

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(
                SitemapNs + "urlset",
                new XAttribute(XNamespace.Xmlns + "image", ImageNs.NamespaceName),
                urls));
    }

    /// <summary>Serializa el documento con declaración XML e indentación legible.</summary>
    public static string Serialize(XDocument document)
    {
        var body = document.ToString();
        if (body.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            return body;

        var declaration = document.Declaration?.ToString()
            ?? "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";

        return $"{declaration}{Environment.NewLine}{body}";
    }

    /// <summary>Convierte una ruta relativa en URL absoluta usando el dominio del sitio.</summary>
    public static string ToAbsoluteUrl(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return baseUrl;

        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.ToString();
        }

        if (string.IsNullOrWhiteSpace(baseUrl)) return path;

        var root = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        return new Uri(root, path.TrimStart('/')).ToString();
    }

    /// <summary>Formatea una fecha en UTC con el formato admitido por sitemaps.</summary>
    public static string FormatLastModified(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss'+00:00'", CultureInfo.InvariantCulture);
    }
}
