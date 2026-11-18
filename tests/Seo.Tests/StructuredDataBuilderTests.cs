using System.Text.Json;
using Aplication.DTOs;
using paginaWeb.Seo;

namespace Seo.Tests;

public class StructuredDataBuilderTests
{
    private const string BaseUrl = "https://unautonomo.com";

    private static ArticleResponseDto CreateArticle() => new()
    {
        Title = "Cómo instalar placas solares",
        Slug = "como-instalar-placas-solares",
        Summary = "Guía práctica de autoconsumo.",
        Author = "Ana Pérez",
        AuthorSocialUrl = "https://x.com/anap",
        AuthorAvatarUrl = "/img/autores/ana.webp",
        Tags = ["solar", "autoconsumo", "solar"],
        PublishedAt = new DateTime(2026, 1, 2, 10, 30, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 1, 3, 8, 0, 0, DateTimeKind.Utc),
        ReadingTimeMinutes = 6
    };

    [Fact]
    public void BuildNewsArticle_IncludesRequiredFields()
    {
        var article = CreateArticle();
        var category = new CategoryResponseDto { Name = "Energía", Slug = "energia" };
        var cover = new ImageMetadataResponseDto
        {
            OriginalWidth = 2000,
            OriginalHeight = 1200,
            Variants =
            [
                new ImageVariantDto { Width = 1600, PublicUrl = "https://cdn.example/1600.webp" }
            ]
        };

        var graph = StructuredDataBuilder.BuildNewsArticle(
            article, cover, category, null, BaseUrl, "unautonomo", "/img/logos/logo.svg", 512, 512);

        Assert.Equal("https://schema.org", graph["@context"]);
        Assert.Equal("NewsArticle", graph["@type"]);
        Assert.Equal("Cómo instalar placas solares", graph["headline"]);
        Assert.Equal("2026-01-02T10:30:00+00:00", graph["datePublished"]);
        Assert.Equal("2026-01-03T08:00:00+00:00", graph["dateModified"]);
        Assert.Equal("Energía", graph["articleSection"]);
        Assert.Equal("solar, autoconsumo", graph["keywords"]);
        Assert.Equal("PT6M", graph["timeRequired"]);
        Assert.Equal("https://unautonomo.com/articulos/como-instalar-placas-solares", graph["url"]?.ToString());

        var images = Assert.IsType<Dictionary<string, object?>[]>(graph["image"]);
        var image = Assert.Single(images);
        Assert.Equal("https://cdn.example/1600.webp", image["url"]?.ToString());
        Assert.Equal(1600, Assert.IsType<int>(image["width"]));
        Assert.Equal(960, Assert.IsType<int>(image["height"]));

        var author = Assert.IsType<Dictionary<string, object?>>(graph["author"]);
        Assert.Equal("Person", author["@type"]?.ToString());
        Assert.Equal("Ana Pérez", author["name"]?.ToString());
        Assert.Equal("https://x.com/anap", author["url"]?.ToString());
        Assert.Equal("https://unautonomo.com/img/autores/ana.webp", author["image"]?.ToString());
    }

    [Fact]
    public void BuildNewsArticle_PrefersLargestVariantOverCoverImageUrl()
    {
        var article = CreateArticle();
        article.CoverImageUrl = "https://cdn.example/800.webp";

        var cover = new ImageMetadataResponseDto
        {
            OriginalWidth = 1920,
            OriginalHeight = 1080,
            Variants =
            [
                new ImageVariantDto { Width = 400, PublicUrl = "https://cdn.example/400.webp" },
                new ImageVariantDto { Width = 800, PublicUrl = "https://cdn.example/800.webp" },
                new ImageVariantDto { Width = 1600, PublicUrl = "https://cdn.example/1600.webp" }
            ]
        };

        var graph = StructuredDataBuilder.BuildNewsArticle(
            article, cover, null, null, BaseUrl, "unautonomo", null, 0, 0);

        var image = Assert.Single(Assert.IsType<Dictionary<string, object?>[]>(graph["image"]));
        Assert.Equal("https://cdn.example/1600.webp", image["url"]?.ToString());
        Assert.Equal(1600, Assert.IsType<int>(image["width"]));
        Assert.Equal(900, Assert.IsType<int>(image["height"]));
    }

    [Fact]
    public void BuildNewsArticle_FallsBackToCoverImageUrl_WhenNoVariants()
    {
        var article = CreateArticle();
        article.CoverImageUrl = "https://media.example/original.jpg";

        var cover = new ImageMetadataResponseDto { OriginalWidth = 2000, OriginalHeight = 1000 };

        var graph = StructuredDataBuilder.BuildNewsArticle(
            article, cover, null, null, BaseUrl, "unautonomo", null, 0, 0);

        var image = Assert.Single(Assert.IsType<Dictionary<string, object?>[]>(graph["image"]));
        Assert.Equal("https://media.example/original.jpg", image["url"]?.ToString());
        Assert.Equal(2000, Assert.IsType<int>(image["width"]));
        Assert.Equal(1000, Assert.IsType<int>(image["height"]));
    }

    [Fact]
    public void BuildNewsArticle_FallsBackToOrganizationAuthor_WhenAuthorIsEmpty()
    {
        var article = CreateArticle();
        article.Author = string.Empty;

        var graph = StructuredDataBuilder.BuildNewsArticle(
            article, null, null, null, BaseUrl, "unautonomo", null, 0, 0);

        var author = Assert.IsType<Dictionary<string, object?>>(graph["author"]);
        Assert.Equal("Organization", author["@type"]);
        Assert.Equal("unautonomo", author["name"]);
    }

    [Fact]
    public void BuildBreadcrumbList_NumbersItemsAndBuildsAbsoluteUrls()
    {
        var graph = StructuredDataBuilder.BuildBreadcrumbList(
            [
                new BreadcrumbItem("Inicio", "/"),
                new BreadcrumbItem("Energía", "/categoria/energia"),
                new BreadcrumbItem("Artículo")
            ],
            BaseUrl);

        Assert.Equal("BreadcrumbList", graph["@type"]?.ToString());
        var elements = Assert.IsType<List<Dictionary<string, object?>>>(graph["itemListElement"]);
        Assert.Equal(3, elements.Count);
        Assert.Equal(1, Assert.IsType<int>(elements[0]["position"]));
        Assert.Equal("https://unautonomo.com/", elements[0]["item"]?.ToString());
        Assert.Equal(2, Assert.IsType<int>(elements[1]["position"]));
        Assert.Equal("https://unautonomo.com/categoria/energia", elements[1]["item"]?.ToString());
        Assert.False(elements[2].ContainsKey("item"));
    }

    [Fact]
    public void BuildWebSite_AddsSearchAction()
    {
        var graph = StructuredDataBuilder.BuildWebSite(BaseUrl, "unautonomo");

        Assert.Equal("WebSite", graph["@type"]?.ToString());
        var action = Assert.IsType<Dictionary<string, object?>>(graph["potentialAction"]);
        Assert.Equal("SearchAction", action["@type"]?.ToString());
        var target = Assert.IsType<Dictionary<string, object?>>(action["target"]);
        Assert.Equal(
            "https://unautonomo.com/publicaciones?q={search_term_string}",
            target["urlTemplate"]?.ToString());
        Assert.Equal("required name=search_term_string", action["query-input"]?.ToString());
    }

    [Fact]
    public void BuildOrganization_IncludesLogoAndSocialProfiles()
    {
        var graph = StructuredDataBuilder.BuildOrganization(
            BaseUrl,
            "unautonomo",
            "/img/logos/logo.svg",
            512,
            512,
            ["https://x.com/unautonomo", "https://x.com/unautonomo", ""]);

        var logo = Assert.IsType<Dictionary<string, object?>>(graph["logo"]);
        Assert.Equal("https://unautonomo.com/img/logos/logo.svg", logo["url"]?.ToString());
        Assert.Equal(512, Assert.IsType<int>(logo["width"]));

        var sameAs = Assert.IsType<List<string>>(graph["sameAs"]);
        Assert.Equal(new[] { "https://x.com/unautonomo" }, sameAs);
    }

    [Fact]
    public void BuildCollectionPage_BuildsItemList()
    {
        var graph = StructuredDataBuilder.BuildCollectionPage(
            "Energía",
            "Noticias de energía solar.",
            "/categoria/energia?page=2",
            [
                new ArticleListItem("Artículo A", "/articulos/a"),
                new ArticleListItem("Artículo B", "/articulos/b")
            ],
            BaseUrl);

        Assert.Equal("CollectionPage", graph["@type"]?.ToString());
        Assert.Equal("https://unautonomo.com/categoria/energia?page=2", graph["url"]?.ToString());

        var itemList = Assert.IsType<Dictionary<string, object?>>(graph["mainEntity"]);
        Assert.Equal("ItemList", itemList["@type"]?.ToString());
        Assert.Equal(2, Assert.IsType<int>(itemList["numberOfItems"]));

        var elements = Assert.IsType<List<Dictionary<string, object?>>>(itemList["itemListElement"]);
        Assert.Equal("https://unautonomo.com/articulos/a", elements[0]["url"]?.ToString());
        Assert.Equal(2, Assert.IsType<int>(elements[1]["position"]));
    }

    [Fact]
    public void BuildWebPage_IncludesDateModified()
    {
        var graph = StructuredDataBuilder.BuildWebPage(
            "Política de privacidad",
            "Cómo tratamos tus datos.",
            "/info/politica-de-privacidad",
            new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc),
            BaseUrl);

        Assert.Equal("WebPage", graph["@type"]?.ToString());
        Assert.Equal("2026-02-01T09:00:00+00:00", graph["dateModified"]?.ToString());
    }

    [Fact]
    public void Render_ProducesValidJsonWrappedInScriptTag()
    {
        var graph = StructuredDataBuilder.BuildWebSite(BaseUrl, "unautonomo");
        var html = JsonLd.Render(graph).Value ?? string.Empty;

        Assert.StartsWith("<script type=\"application/ld+json\">", html);
        Assert.EndsWith("</script>", html);

        var json = html[(html.IndexOf('>') + 1)..^"</script>".Length];
        using var document = JsonDocument.Parse(json);
        Assert.Equal("https://schema.org", document.RootElement.GetProperty("@context").GetString());
    }

    [Fact]
    public void Render_EscapesClosingScriptTags()
    {
        var html = JsonLd.Render(new { note = "</script><script>alert(1)</script>" }).Value ?? string.Empty;

        Assert.Contains("<\\/script>", html);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "</script>"));
    }
}
