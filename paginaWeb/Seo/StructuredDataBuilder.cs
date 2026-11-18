using System.Globalization;
using Aplication.DTOs;

namespace paginaWeb.Seo;

/// <summary>Elemento de una miga de pan (<c>BreadcrumbList</c>).</summary>
public sealed record BreadcrumbItem(string Name, string? Url = null);

/// <summary>Entrada de un listado de artículos para <c>ItemList</c>.</summary>
public sealed record ArticleListItem(string Name, string Url);

/// <summary>
/// Construye grafos Schema.org (JSON-LD) a partir de los DTOs de la aplicación.
/// No depende de <see cref="HttpContext"/>: recibe la URL base y los datos ya cargados,
/// lo que mantiene los builders puros y testables.
/// </summary>
public static class StructuredDataBuilder
{
    private const string Context = "https://schema.org";
    private const string Language = "es-ES";

    // ── Site-wide ─────────────────────────────────────────────────────────────

    public static Dictionary<string, object?> BuildOrganization(
        string baseUrl,
        string siteName,
        string? logoUrl,
        int logoWidth,
        int logoHeight,
        IEnumerable<string>? sameAs = null)
    {
        var organization = new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["name"] = siteName,
            ["url"] = baseUrl
        };

        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            // Nota: Google recomienda un logotipo raster (PNG/JPEG ≥ 112×112).
            // Se usa el SVG del sitio; si se añade un PNG, basta con cambiar SiteBrand.LogoPath.
            organization["logo"] = new Dictionary<string, object?>
            {
                ["@type"] = "ImageObject",
                ["url"] = SitemapDocumentBuilder.ToAbsoluteUrl(baseUrl, logoUrl),
                ["width"] = logoWidth,
                ["height"] = logoHeight
            };
        }

        var profiles = sameAs?
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (profiles is { Count: > 0 })
            organization["sameAs"] = profiles;

        return Wrap(organization);
    }

    public static Dictionary<string, object?> BuildWebSite(string baseUrl, string siteName)
    {
        var website = new Dictionary<string, object?>
        {
            ["@type"] = "WebSite",
            ["name"] = siteName,
            ["url"] = baseUrl,
            ["inLanguage"] = Language,
            ["potentialAction"] = new Dictionary<string, object?>
            {
                ["@type"] = "SearchAction",
                ["target"] = new Dictionary<string, object?>
                {
                    ["@type"] = "EntryPoint",
                    ["urlTemplate"] = $"{baseUrl}/publicaciones?q={{search_term_string}}"
                },
                ["query-input"] = "required name=search_term_string"
            }
        };

        return Wrap(website);
    }

    // ── Artículo ──────────────────────────────────────────────────────────────

    public static Dictionary<string, object?> BuildNewsArticle(
        ArticleResponseDto article,
        ImageMetadataResponseDto? coverImage,
        CategoryResponseDto? category,
        CategoryResponseDto? subcategory,
        string baseUrl,
        string siteName,
        string? logoUrl,
        int logoWidth,
        int logoHeight)
    {
        var articleUrl = SitemapDocumentBuilder.ToAbsoluteUrl(baseUrl, $"/articulos/{article.Slug}");

        var graph = new Dictionary<string, object?>
        {
            ["@type"] = "NewsArticle",
            ["mainEntityOfPage"] = new Dictionary<string, object?>
            {
                ["@type"] = "WebPage",
                ["@id"] = articleUrl
            },
            ["headline"] = article.Title,
            ["url"] = articleUrl,
            ["datePublished"] = FormatIso8601(article.PublishedAt ?? article.CreatedAt),
            ["dateModified"] = FormatIso8601(article.UpdatedAt),
            ["inLanguage"] = Language,
            ["isAccessibleForFree"] = true,
            ["author"] = BuildAuthor(article, siteName, baseUrl),
            ["publisher"] = BuildPublisher(siteName, baseUrl, logoUrl, logoWidth, logoHeight)
        };

        if (!string.IsNullOrWhiteSpace(article.Summary))
            graph["description"] = article.Summary;

        var (coverUrl, coverWidth, coverHeight) = ResolveCover(article, coverImage);
        if (!string.IsNullOrWhiteSpace(coverUrl))
        {
            var image = new Dictionary<string, object?>
            {
                ["@type"] = "ImageObject",
                ["url"] = SitemapDocumentBuilder.ToAbsoluteUrl(baseUrl, coverUrl)
            };

            if (coverWidth > 0 && coverHeight > 0)
            {
                image["width"] = coverWidth;
                image["height"] = coverHeight;
            }

            graph["image"] = new[] { image };
        }

        var section = !string.IsNullOrWhiteSpace(category?.Name)
            ? category!.Name
            : subcategory?.Name;

        if (!string.IsNullOrWhiteSpace(section))
            graph["articleSection"] = section;

        var keywords = article.Tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (keywords.Count > 0)
            graph["keywords"] = string.Join(", ", keywords);

        if (article.ReadingTimeMinutes > 0)
            graph["timeRequired"] = $"PT{article.ReadingTimeMinutes}M";

        return Wrap(graph);
    }

    public static Dictionary<string, object?> BuildBreadcrumbList(
        IEnumerable<BreadcrumbItem> items,
        string baseUrl)
    {
        var elements = items
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select((item, index) =>
            {
                var element = new Dictionary<string, object?>
                {
                    ["@type"] = "ListItem",
                    ["position"] = index + 1,
                    ["name"] = item.Name
                };

                if (!string.IsNullOrWhiteSpace(item.Url))
                    element["item"] = SitemapDocumentBuilder.ToAbsoluteUrl(baseUrl, item.Url);

                return element;
            })
            .ToList();

        return Wrap(new Dictionary<string, object?>
        {
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = elements
        });
    }

    // ── Listados y páginas informativas ───────────────────────────────────────

    public static Dictionary<string, object?> BuildCollectionPage(
        string name,
        string? description,
        string url,
        IEnumerable<ArticleListItem> items,
        string baseUrl)
    {
        var elements = items
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select((item, index) => new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = index + 1,
                ["name"] = item.Name,
                ["url"] = SitemapDocumentBuilder.ToAbsoluteUrl(baseUrl, item.Url)
            })
            .ToList();

        var graph = new Dictionary<string, object?>
        {
            ["@type"] = "CollectionPage",
            ["name"] = name,
            ["url"] = SitemapDocumentBuilder.ToAbsoluteUrl(baseUrl, url),
            ["inLanguage"] = Language,
            ["mainEntity"] = new Dictionary<string, object?>
            {
                ["@type"] = "ItemList",
                ["numberOfItems"] = elements.Count,
                ["itemListElement"] = elements
            }
        };

        if (!string.IsNullOrWhiteSpace(description))
            graph["description"] = description;

        return Wrap(graph);
    }

    public static Dictionary<string, object?> BuildWebPage(
        string name,
        string? description,
        string url,
        DateTime? dateModified,
        string baseUrl,
        string type = "WebPage")
    {
        var graph = new Dictionary<string, object?>
        {
            ["@type"] = type,
            ["name"] = name,
            ["url"] = SitemapDocumentBuilder.ToAbsoluteUrl(baseUrl, url),
            ["inLanguage"] = Language
        };

        if (!string.IsNullOrWhiteSpace(description))
            graph["description"] = description;

        if (dateModified is not null)
            graph["dateModified"] = FormatIso8601(dateModified.Value);

        return Wrap(graph);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Dictionary<string, object?> BuildAuthor(
        ArticleResponseDto article,
        string siteName,
        string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(article.Author))
        {
            return new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = siteName,
                ["url"] = baseUrl
            };
        }

        var author = new Dictionary<string, object?>
        {
            ["@type"] = "Person",
            ["name"] = article.Author
        };

        if (!string.IsNullOrWhiteSpace(article.AuthorSocialUrl))
            author["url"] = SitemapDocumentBuilder.ToAbsoluteUrl(baseUrl, article.AuthorSocialUrl);

        if (!string.IsNullOrWhiteSpace(article.AuthorAvatarUrl))
            author["image"] = SitemapDocumentBuilder.ToAbsoluteUrl(baseUrl, article.AuthorAvatarUrl);

        return author;
    }

    private static Dictionary<string, object?> BuildPublisher(
        string siteName,
        string baseUrl,
        string? logoUrl,
        int logoWidth,
        int logoHeight)
    {
        var publisher = new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["name"] = siteName,
            ["url"] = baseUrl
        };

        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            publisher["logo"] = new Dictionary<string, object?>
            {
                ["@type"] = "ImageObject",
                ["url"] = SitemapDocumentBuilder.ToAbsoluteUrl(baseUrl, logoUrl),
                ["width"] = logoWidth,
                ["height"] = logoHeight
            };
        }

        return publisher;
    }

    private static (string? Url, int Width, int Height) ResolveCover(
        ArticleResponseDto article,
        ImageMetadataResponseDto? coverImage)
    {
        // Preferimos la variante de mayor resolución: es la que Google usa como imagen
        // destacada y sus dimensiones son las reales (CoverImageUrl suele ser un derivado).
        var best = coverImage?.Variants
            .Where(variant => !string.IsNullOrWhiteSpace(variant.PublicUrl) && variant.Width > 0)
            .OrderByDescending(variant => variant.Width)
            .FirstOrDefault();

        if (best is not null)
        {
            var height = coverImage!.OriginalWidth > 0 && coverImage.OriginalHeight > 0
                ? (int)Math.Round(coverImage.OriginalHeight * (double)best.Width / coverImage.OriginalWidth)
                : 0;

            return (best.PublicUrl, best.Width, height);
        }

        if (!string.IsNullOrWhiteSpace(article.CoverImageUrl))
        {
            // Sin variantes conocidas: asumimos que la URL apunta al original.
            return (
                article.CoverImageUrl,
                coverImage?.OriginalWidth ?? 0,
                coverImage?.OriginalHeight ?? 0);
        }

        return (null, 0, 0);
    }

    private static string FormatIso8601(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss'+00:00'", CultureInfo.InvariantCulture);
    }

    private static Dictionary<string, object?> Wrap(Dictionary<string, object?> graph)
    {
        var result = new Dictionary<string, object?> { ["@context"] = Context };
        foreach (var (key, value) in graph)
            result[key] = value;

        return result;
    }
}
