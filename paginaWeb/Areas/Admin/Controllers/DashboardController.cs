using Aplication.Services;
using Aplication.Mappers;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DashboardController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ICommentService _commentService;
    private readonly ImageService _imageService;

    public DashboardController(
        IArticleService articleService,
        ICategoryService categoryService,
        ICommentService commentService,
        ImageService imageService)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _commentService = commentService;
        _imageService = imageService;
    }

    [Route("admin")]
    [Route("admin/dashboard")]
    public async Task<IActionResult> Index()
    {
        var articles = await _articleService.GetAllAdminAsync(1, 10);
        var (_, totalArticles) = await _articleService.GetPublishedAsync(1, 1);
        var categories = await _categoryService.GetAllAsync();
        var pendingComments = await _commentService.GetPendingAsync();

        ViewBag.TotalArticles = totalArticles;
        ViewBag.TotalCategories = categories.Count;
        ViewBag.PendingCommentsCount = pendingComments.Count;
        ViewBag.PendingComments = pendingComments.Take(5).ToList();
        ViewBag.RecentArticles = articles.Take(5).ToList();

        var coverIds = articles.Where(a => !string.IsNullOrEmpty(a.CoverImageId)).Select(a => a.CoverImageId!).Distinct().ToList();
        var images = await _imageService.GetVariantsDictAsync(coverIds);
        ViewBag.CoverImages = images.ToDictionary(
            kvp => kvp.Key,
            kvp => ImageMapper.GetThumbnailUrl(kvp.Value) ?? ImageMapper.GetBestUrl(kvp.Value) ?? string.Empty
        );

        return View();
    }
}
