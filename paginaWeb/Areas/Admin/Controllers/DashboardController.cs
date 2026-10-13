using Aplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DashboardController : Controller
{
    private readonly IArticleRepository _articleRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ICommentRepository _commentRepo;
    private readonly IImageMetadataRepository _imageRepo;

    public DashboardController(
        IArticleRepository articleRepo,
        ICategoryRepository categoryRepo,
        ICommentRepository commentRepo,
        IImageMetadataRepository imageRepo)
    {
        _articleRepo = articleRepo;
        _categoryRepo = categoryRepo;
        _commentRepo = commentRepo;
        _imageRepo = imageRepo;
    }

    [Route("admin")]
    [Route("admin/dashboard")]
    public async Task<IActionResult> Index()
    {
        var articles = await _articleRepo.GetAllAsync(0, 10);
        var totalArticles = await _articleRepo.CountPublishedAsync();
        var categories = await _categoryRepo.GetAllAsync();
        var pendingComments = await _commentRepo.GetPendingAsync();
        var images = await _imageRepo.GetAllAsync(0, 10);

        ViewBag.TotalArticles = totalArticles;
        ViewBag.TotalCategories = categories.Count;
        ViewBag.PendingCommentsCount = pendingComments.Count;
        ViewBag.PendingComments = pendingComments.Take(5).ToList();
        ViewBag.RecentArticles = articles.Take(5).ToList();

        return View();
    }
}
