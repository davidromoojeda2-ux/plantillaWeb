using System.Xml.Linq;
using paginaWeb.Seo;

namespace ContenidoBloques.Tests;

public class SitemapDocumentBuilderTests
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace ImageNs = "http://www.google.com/schemas/sitemap-image/1.1";

    [Fact]
    public void BuildIndex_GeneraLaEstructuraDeSitemapIndex()
    {
        var document = SitemapDocumentBuilder.BuildIndex(
            "https://www.ejemplo.com",
            [
                new("/sitemaps/paginas.xml", new DateTime(2026, 10, 28, 9, 15, 0, DateTimeKind.Utc)),
                new("/sitemaps/blog.xml", new DateTime(2026, 10, 27, 18, 2, 0, DateTimeKind.Utc))
            ]);

        var root = Assert.IsType<XElement>(document.Root);
        Assert.Equal(SitemapNs + "sitemapindex", root.Name);

        var sitemaps = root.Elements(SitemapNs + "sitemap").ToList();
        Assert.Equal(2, sitemaps.Count);
        Assert.Equal(
            "https://www.ejemplo.com/sitemaps/paginas.xml",
            sitemaps[0].Element(SitemapNs + "loc")?.Value);
        Assert.Equal(
            "2026-10-28T09:15:00+00:00",
            sitemaps[0].Element(SitemapNs + "lastmod")?.Value);
    }

    [Fact]
    public void BuildUrlSet_IncluyeLasUrlsDeLasImagenes()
    {
        var document = SitemapDocumentBuilder.BuildUrlSet(
            "https://www.ejemplo.com",
            [
                new(
                    "/articulos/inauguran-centro",
                    new DateTime(2026, 10, 28, 8, 40, 0, DateTimeKind.Utc),
                    [
                        new("/uploads/portada.webp", "Portada del artículo", "Inauguran centro"),
                        new("https://cdn.ejemplo.com/imagen.webp")
                    ])
            ]);

        var root = Assert.IsType<XElement>(document.Root);
        Assert.Equal(SitemapNs + "urlset", root.Name);

        var url = Assert.Single(root.Elements(SitemapNs + "url"));
        Assert.Equal(
            "https://www.ejemplo.com/articulos/inauguran-centro",
            url.Element(SitemapNs + "loc")?.Value);

        var images = url.Elements(ImageNs + "image").ToList();
        Assert.Equal(2, images.Count);
        Assert.Equal(
            "https://www.ejemplo.com/uploads/portada.webp",
            images[0].Element(ImageNs + "loc")?.Value);
        Assert.Equal(
            "Portada del artículo",
            images[0].Element(ImageNs + "caption")?.Value);
        Assert.Equal(
            "https://cdn.ejemplo.com/imagen.webp",
            images[1].Element(ImageNs + "loc")?.Value);
    }

    [Fact]
    public void Serialize_AgregaLaDeclaracionXmlYElNamespaceDeImagenes()
    {
        var document = SitemapDocumentBuilder.BuildUrlSet(
            "https://www.ejemplo.com",
            [new("/", null, [new("/uploads/imagen.webp")])]);

        var xml = SitemapDocumentBuilder.Serialize(document);

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", xml);
        Assert.Contains("xmlns:image=\"http://www.google.com/schemas/sitemap-image/1.1\"", xml);
    }

    [Theory]
    [InlineData("/articulos/nota", "https://www.ejemplo.com/articulos/nota")]
    [InlineData("https://cdn.ejemplo.com/foto.webp", "https://cdn.ejemplo.com/foto.webp")]
    public void ToAbsoluteUrl_ResuelveRutasRelativasYRespetaUrlsAbsolutas(string path, string expected)
    {
        Assert.Equal(expected, SitemapDocumentBuilder.ToAbsoluteUrl("https://www.ejemplo.com", path));
    }
}
