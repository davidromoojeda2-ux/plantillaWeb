using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.Controllers;

public class HomeController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ICommentService _commentService;
    private readonly IPageService _pageService;
    private readonly IImageMetadataRepository _imageRepo;

    public HomeController(
        IArticleService articleService,
        ICategoryRepository categoryRepo,
        ICommentService commentService,
        IPageService pageService,
        IImageMetadataRepository imageRepo)
    {
        _articleService = articleService;
        _categoryRepo = categoryRepo;
        _commentService = commentService;
        _pageService = pageService;
        _imageRepo = imageRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var featured = await _articleService.GetFeaturedAsync(3);
        var (recent, total) = await _articleService.GetPublishedAsync(1, 9);
        var categories = await _categoryRepo.GetAllAsync();

        ViewBag.Featured = featured;
        ViewBag.Categories = categories.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.Images = await LoadImageVariantsDictAsync(recent.Concat(featured).Select(a => a.CoverImageId).Where(id => !string.IsNullOrEmpty(id))!);

        return View(recent);
    }

    [HttpGet("categoria/{slug}")]
    public async Task<IActionResult> Categoria(string slug, int page = 1)
    {
        var category = await _categoryRepo.GetBySlugAsync(slug);
        if (category is null) return NotFound();

        var (articles, total) = await _articleService.GetByCategoryAsync(category.Id, page, 9);
        var categories = await _categoryRepo.GetAllAsync();

        ViewBag.Category = category;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / 9);
        ViewBag.TotalCount = total;
        ViewBag.Categories = categories.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.Images = await LoadImageVariantsDictAsync(articles.Select(a => a.CoverImageId).Where(id => !string.IsNullOrEmpty(id))!);

        return View(articles);
    }

    [HttpGet("articulos/{slug}")]
    public async Task<IActionResult> Articulo(string slug)
    {
        var article = await _articleService.GetBySlugAsync(slug);
        if (article is null || article.Status != ArticleStatus.Publicado)
            return NotFound();

        await _articleService.IncrementViewCountAsync(article.Id);

        var comments = await _commentService.GetApprovedByArticleAsync(article.Id);
        var category = !string.IsNullOrEmpty(article.CategoryId)
            ? await _categoryRepo.GetByIdAsync(article.CategoryId)
            : null;

        ImageMetadata? coverImage = null;
        if (!string.IsNullOrEmpty(article.CoverImageId))
        {
            coverImage = await _imageRepo.GetByIdAsync(article.CoverImageId);
        }

        // Estimación de tiempo de lectura (palabras / 200)
        var wordCount = (article.Content ?? "").Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
        var readingMinutes = Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));

        ViewBag.Comments = comments;
        ViewBag.Category = category;
        ViewBag.CoverImage = coverImage;
        ViewBag.ReadingMinutes = readingMinutes;

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
    public async Task<IActionResult> Buscar(string? q, int page = 1)
    {
        var query = q?.Trim() ?? string.Empty;
        var (items, total) = string.IsNullOrEmpty(query)
            ? (new List<Article>(), 0L)
            : await _articleService.SearchAsync(query, page, 9);

        var categories = await _categoryRepo.GetAllAsync();

        ViewBag.Query = query;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / 9);
        ViewBag.TotalCount = total;
        ViewBag.Categories = categories.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.Images = await LoadImageVariantsDictAsync(items.Select(a => a.CoverImageId).Where(id => !string.IsNullOrEmpty(id))!);

        return View(items);
    }

    [HttpGet("info/{slug}")]
    public async Task<IActionResult> Info(string slug)
    {
        var page = await _pageService.GetBySlugAsync(slug);
        if (page is null) return NotFound();

        var allPages = await _pageService.GetFooterPagesAsync();
        ViewBag.OtherPages = allPages;

        return View(page);
    }

    private async Task<Dictionary<string, ImageMetadata>> LoadImageVariantsDictAsync(IEnumerable<string> ids)
    {
        var dict = new Dictionary<string, ImageMetadata>();
        foreach (var id in ids.Distinct())
        {
            var img = await _imageRepo.GetByIdAsync(id);
            if (img != null) dict[id] = img;
        }
        return dict;
    }
}
