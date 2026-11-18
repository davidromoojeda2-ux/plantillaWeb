using paginaWeb.Seo;

namespace Seo.Tests;

public class CanonicalUrlTests
{
    private const string BaseUrl = "https://unautonomo.com";

    private static IEnumerable<KeyValuePair<string, string?>> Query(params (string Key, string? Value)[] pairs)
        => pairs.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value));

    [Fact]
    public void Build_ReturnsRootUrl()
    {
        var url = CanonicalUrl.Build(BaseUrl, "/");

        Assert.Equal("https://unautonomo.com/", url);
    }

    [Fact]
    public void Build_DropsTrackingParameters()
    {
        var url = CanonicalUrl.Build(
            BaseUrl,
            "/articulos/placas-solares",
            Query(("utm_source", "x"), ("utm_medium", "social"), ("ref", "home")));

        Assert.Equal("https://unautonomo.com/articulos/placas-solares", url);
    }

    [Fact]
    public void Build_KeepsPagination()
    {
        var url = CanonicalUrl.Build(
            BaseUrl,
            "/categoria/energia",
            Query(("page", "3"), ("utm_source", "x")));

        Assert.Equal("https://unautonomo.com/categoria/energia?page=3", url);
    }

    [Fact]
    public void Build_DropsFirstPageParameter()
    {
        var url = CanonicalUrl.Build(BaseUrl, "/publicaciones", Query(("page", "1")));

        Assert.Equal("https://unautonomo.com/publicaciones", url);
    }

    [Fact]
    public void Build_IgnoresSearchQuery()
    {
        var url = CanonicalUrl.Build(
            BaseUrl,
            "/publicaciones",
            Query(("q", "baterías"), ("page", "2")));

        Assert.Equal("https://unautonomo.com/publicaciones?page=2", url);
    }

    [Fact]
    public void Build_TrimsTrailingSlashFromBaseUrlAndNormalizesPath()
    {
        var url = CanonicalUrl.Build("https://unautonomo.com/", "info/aviso-legal");

        Assert.Equal("https://unautonomo.com/info/aviso-legal", url);
    }

    [Fact]
    public void Build_EscapesQueryValuesAndHonoursCustomKeys()
    {
        var url = CanonicalUrl.Build(
            BaseUrl,
            "/publicaciones",
            Query(("orden", "más vistos"), ("page", "2")),
            ["orden"]);

        Assert.Equal("https://unautonomo.com/publicaciones?orden=m%C3%A1s%20vistos", url);
    }
}
