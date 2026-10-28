using System.Text;
using System.Xml.Linq;
using Aplication.DTOs;
using Aplication.DTOs.ContentBlocks;
using Aplication.Mappers;
using Aplication.Services;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;
using paginaWeb.Seo;

namespace paginaWeb.Controllers;

/// <summary>
/// Endpoints públicos de indexación: robots.txt, índice de sitemaps y sitemaps
/// parciales de páginas, artículos y categorías.
/// </summary>
[Route("")]
public class SeoController : Controller
{
    private const string PagesSitemapPath = "/sitemaps/paginas.xml";
    private const string BlogSitemapPath = "/sitemaps/blog.xml";
    private const string CategoriesSitemapPath = "/sitemaps/categorias.xml";

    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly IPageService _pageService;
    private readonly ImageService _imageService;
    private readonly IConfiguration _configuration;

    public SeoController(
        IArticleService articleService,
        ICategoryService categoryService,
        IPageService pageService,
        ImageService imageService,
        IConfiguration configuration)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _pageService = pageService;
        _imageService = imageService;
        _configuration = configuration;
    }

    [HttpGet("robots.txt")]
    public IActionResult Robots()
    {
        var baseUrl = GetBaseUrl();
        var robots = new StringBuilder()
            .AppendLine("User-agent: *")
            .AppendLine("Allow: /")
            .AppendLine()
            .AppendLine("# Panel de administración")
            .AppendLine("Disallow: /admin")
            .AppendLine("Disallow: /admin/")
            .AppendLine()
            .AppendLine($"Sitemap: {baseUrl}/sitemap.xml")
            .ToString();

        return Content(robots, "text/plain; charset=utf-8", Encoding.UTF8);
    }

    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> SitemapIndex()
    {
        var articles = await _articleService.GetAllPublishedAsync();
        var pages = await _pageService.GetAllAsync();
        var categories = await _categoryService.GetAllAsync();
        var categoryIdsWithArticles = await _articleService.GetCategoriesWithPublishedArticlesAsync();

        var indexableCategories = categories
            .Where(category => category.IsActive && categoryIdsWithArticles.Contains(category.Id))
            .ToList();

        var entries = new List<SitemapIndexEntry>
        {
            new(PagesSitemapPath, Latest(pages.Select(page => page.UpdatedAt))),
            new(BlogSitemapPath, Latest(articles.Select(article => article.UpdatedAt))),
            new(CategoriesSitemapPath, Latest(indexableCategories.Select(category => category.UpdatedAt)))
        };

        return SitemapResult(SitemapDocumentBuilder.BuildIndex(GetBaseUrl(), entries));
    }

    [HttpGet("sitemaps/paginas.xml")]
    public async Task<IActionResult> PagesSitemap()
    {
        var pages = await _pageService.GetAllAsync();
        var articles = await _articleService.GetAllPublishedAsync();

        var imageIds = pages
            .SelectMany(page => CollectImageIds(page.Blocks))
            .Distinct()
            .ToList();

        var images = await _imageService.GetVariantsDictAsync(imageIds);
        var latestArticleUpdate = Latest(articles.Select(article => article.UpdatedAt));

        var entries = new List<SitemapUrlEntry>
        {
            new("/", latestArticleUpdate),
            new("/publicaciones", latestArticleUpdate)
        };

        entries.AddRange(pages.Select(page => new SitemapUrlEntry(
            $"/info/{page.Slug}",
            page.UpdatedAt,
            BuildImagesFromBlocks(page.Blocks, images))));

        return SitemapResult(SitemapDocumentBuilder.BuildUrlSet(GetBaseUrl(), entries));
    }

    [HttpGet("sitemaps/blog.xml")]
    public async Task<IActionResult> BlogSitemap()
    {
        var articles = await _articleService.GetAllPublishedAsync();

        var imageIds = articles
            .SelectMany(CollectImageIds)
            .Distinct()
            .ToList();

        var images = await _imageService.GetVariantsDictAsync(imageIds);

        var entries = articles.Select(article => new SitemapUrlEntry(
            $"/articulos/{article.Slug}",
            article.UpdatedAt,
            BuildArticleImages(article, images)));

        return SitemapResult(SitemapDocumentBuilder.BuildUrlSet(GetBaseUrl(), entries));
    }

    [HttpGet("sitemaps/categorias.xml")]
    public async Task<IActionResult> CategoriesSitemap()
    {
        var categories = await _categoryService.GetAllAsync();
        var categoryIdsWithArticles = await _articleService.GetCategoriesWithPublishedArticlesAsync();

        var entries = categories
            .Where(category => category.IsActive && categoryIdsWithArticles.Contains(category.Id))
            .Select(category => new SitemapUrlEntry($"/categoria/{category.Slug}", category.UpdatedAt));

        return SitemapResult(SitemapDocumentBuilder.BuildUrlSet(GetBaseUrl(), entries));
    }

    private IActionResult SitemapResult(XDocument document) =>
        Content(
            SitemapDocumentBuilder.Serialize(document),
            "application/xml; charset=utf-8",
            Encoding.UTF8);

    private string GetBaseUrl()
    {
        var configured = _configuration["Site:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');

        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
    }

    private static IEnumerable<string> CollectImageIds(ArticleResponseDto article)
    {
        if (!string.IsNullOrWhiteSpace(article.CoverImageId))
            yield return article.CoverImageId;

        foreach (var imageId in article.ImageIds.Where(id => !string.IsNullOrWhiteSpace(id)))
            yield return imageId;

        foreach (var imageId in CollectImageIds(article.Blocks))
            yield return imageId;
    }

    private static IEnumerable<string> CollectImageIds(IEnumerable<ContentBlockDto> blocks) =>
        blocks
            .OfType<ContentImageBlockDto>()
            .Select(block => block.ImageId)
            .Where(imageId => !string.IsNullOrWhiteSpace(imageId))!;

    private static IReadOnlyCollection<SitemapImage> BuildArticleImages(
        ArticleResponseDto article,
        IReadOnlyDictionary<string, ImageMetadataResponseDto> images)
    {
        var result = new List<SitemapImage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var coverUrl = !string.IsNullOrWhiteSpace(article.CoverImageUrl)
            ? article.CoverImageUrl
            : ImageMapper.GetBestUrl(GetImage(images, article.CoverImageId));

        AddImage(result, seen, coverUrl, article.Title, caption: null);

        foreach (var block in article.Blocks.OfType<ContentImageBlockDto>())
        {
            var image = GetImage(images, block.ImageId);
            var url = image is not null ? ImageMapper.GetBestUrl(image) : block.Url;
            AddImage(result, seen, url, article.Title, block.Alt);
        }

        foreach (var imageId in article.ImageIds)
        {
            var image = GetImage(images, imageId);
            AddImage(result, seen, ImageMapper.GetBestUrl(image), article.Title, image?.Alt);
        }

        return result;
    }

    private static IReadOnlyCollection<SitemapImage> BuildImagesFromBlocks(
        IEnumerable<ContentBlockDto> blocks,
        IReadOnlyDictionary<string, ImageMetadataResponseDto> images)
    {
        var result = new List<SitemapImage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var block in blocks.OfType<ContentImageBlockDto>())
        {
            var image = GetImage(images, block.ImageId);
            var url = image is not null ? ImageMapper.GetBestUrl(image) : block.Url;
            AddImage(result, seen, url, title: null, block.Alt);
        }

        return result;
    }

    private static ImageMetadataResponseDto? GetImage(
        IReadOnlyDictionary<string, ImageMetadataResponseDto> images,
        string? imageId) =>
        !string.IsNullOrWhiteSpace(imageId) && images.TryGetValue(imageId, out var image)
            ? image
            : null;

    private static void AddImage(
        ICollection<SitemapImage> images,
        ISet<string> seen,
        string? url,
        string? title,
        string? caption)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!seen.Add(url.Trim())) return;

        images.Add(new SitemapImage(
            url.Trim(),
            string.IsNullOrWhiteSpace(caption) ? null : caption.Trim(),
            string.IsNullOrWhiteSpace(title) ? null : title.Trim()));
    }

    private static DateTime? Latest(IEnumerable<DateTime> values)
    {
        var list = values.Where(value => value != default).ToList();
        return list.Count == 0 ? null : list.Max();
    }
}
