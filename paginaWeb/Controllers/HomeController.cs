using Aplication.DTOs;
using Aplication.DTOs.ContentBlocks;
using Aplication.Mappers;
using Aplication.Services;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;
using paginaWeb.Models.Category;
using paginaWeb.Models.Home;

namespace paginaWeb.Controllers;

public class HomeController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ICommentService _commentService;
    private readonly IPageService _pageService;
    private readonly ImageService _imageService;

    public HomeController(
        IArticleService articleService,
        ICategoryService categoryService,
        ICommentService commentService,
        IPageService pageService,
        ImageService imageService)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _commentService = commentService;
        _pageService = pageService;
        _imageService = imageService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var featured = await _articleService.GetFeaturedAsync(3);
        var mostRead = await _articleService.GetMostReadAsync(5);
        var (recent, _) = await _articleService.GetPublishedAsync(1, 50);
        var categories = await _categoryService.GetAllAsync();
        var images = await _imageService.GetVariantsDictAsync(
            recent.Concat(featured).Concat(mostRead).Select(a => a.CoverImageId).Where(id => !string.IsNullOrEmpty(id))!);

        var model = HomePageViewModelFactory.Create(
            recent,
            featured,
            categories,
            images,
            slug => Url.Action(nameof(Articulo), "Home", new { slug }) ?? "#",
            Url.Action("Index", "Publicaciones"),
            mostRead: mostRead);

        return View(model);
    }


    [HttpGet("articulos/{slug}")]
    public async Task<IActionResult> Articulo(string slug)
    {
        var article = await _articleService.GetBySlugAsync(slug);
        if (article is null || !string.Equals(article.Status, "Publicado", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        await _articleService.IncrementViewCountAsync(article.Id);

        var comments = await _commentService.GetApprovedByArticleAsync(article.Id);
        var category = !string.IsNullOrEmpty(article.CategoryId)
            ? await _categoryService.GetByIdAsync(article.CategoryId)
            : null;

        var subcategory = !string.IsNullOrEmpty(article.SubcategoryId)
            ? await _categoryService.GetByIdAsync(article.SubcategoryId)
            : null;

        ImageMetadataResponseDto? coverImage = null;
        if (!string.IsNullOrEmpty(article.CoverImageId))
        {
            coverImage = await _imageService.GetByIdAsync(article.CoverImageId);
        }

        // Artículos recomendados
        var recommended = await _articleService.GetRecommendedAsync(article.Id, article.CategoryId, 3);

        // Artículos adyacentes (Anterior / Siguiente) para la navegabilidad
        var (prevArticle, nextArticle) = await _articleService.GetAdjacentArticlesAsync(article.Id, article.PublishedAt);

        // Obtener variantes de imágenes para recomendados y artículos adyacentes en una sola llamada
        var bodyImageIds = article.Blocks
            .OfType<ContentImageBlockDto>()
            .Select(b => b.ImageId)
            .Where(id => !string.IsNullOrEmpty(id))!;

        var imageIdsToFetch = recommended
            .Select(a => a.CoverImageId)
            .Concat(new[] { prevArticle?.CoverImageId, nextArticle?.CoverImageId })
            .Concat(bodyImageIds)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var imagesDict = imageIdsToFetch.Count > 0
            ? await _imageService.GetVariantsDictAsync(imageIdsToFetch!)
            : new Dictionary<string, ImageMetadataResponseDto>();

        ViewBag.Comments = comments;
        ViewBag.Category = category;
        ViewBag.Subcategory = subcategory;
        ViewBag.CoverImage = coverImage;
        ViewBag.ReadingMinutes = article.ReadingTimeMinutes;
        ViewBag.RecommendedArticles = recommended;
        ViewBag.RecommendedImages = imagesDict;
        ViewBag.BodyImages = imagesDict;
        ViewBag.PreviousArticle = prevArticle;
        ViewBag.NextArticle = nextArticle;
        ViewBag.AdjacentImages = imagesDict;
        ViewBag.RecommendedCategories = category is not null
            ? new Dictionary<string, string> { { category.Id, category.Name } }
            : new Dictionary<string, string>();

        return View(article);
    }

    [HttpPost("articulos/comentar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comentar(CommentSubmissionDto model, string articleSlug)
    {
        if (!ModelState.IsValid)
        {
            TempData["CommentError"] = "Por favor verifica los campos del comentario.";
            return RedirectToAction(nameof(Articulo), new { slug = articleSlug });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _commentService.SubmitAsync(model, ip);

        TempData["CommentSuccess"] = "¡Gracias por tu comentario! Se ha recibido con éxito y será visible tan pronto sea aprobado por nuestro equipo de moderación.";
        return RedirectToAction(nameof(Articulo), new { slug = articleSlug });
    }

    [HttpGet("buscar")]
    public IActionResult Buscar(string? q, int page = 1)
        => RedirectToAction("Index", "Publicaciones", new { q, page });

    [HttpGet("info/{slug}")]
    public async Task<IActionResult> Info(string slug)
    {
        var page = await _pageService.GetBySlugAsync(slug);
        if (page is null) return NotFound();

        var allPages = await _pageService.GetFooterPagesAsync();
        ViewBag.OtherPages = allPages;

        var bodyImageIds = page.Blocks
            .OfType<ContentImageBlockDto>()
            .Select(b => b.ImageId)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        ViewBag.BodyImages = bodyImageIds.Count > 0
            ? await _imageService.GetVariantsDictAsync(bodyImageIds!)
            : new Dictionary<string, ImageMetadataResponseDto>();

        return View(page);
    }
}
