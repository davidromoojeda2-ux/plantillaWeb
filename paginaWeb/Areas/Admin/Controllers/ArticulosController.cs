using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Mappers;
using Aplication.Services;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/articulos")]
public class ArticulosController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ImageService _imageService;

    public ArticulosController(
        IArticleService articleService,
        ICategoryService categoryService,
        ImageService imageService)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _imageService = imageService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var articles = await _articleService.GetAllAdminAsync(page, 20);
        var categories = await _categoryService.GetAllAsync();
        ViewBag.Categories = categories.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.Page = page;
        return View(articles);
    }

    [HttpGet("nuevo")]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesViewBagAsync();
        var model = new ArticleDto
        {
            Author = User.Identity?.Name ?? "Redacción",
            Status = "Publicado"
        };
        return View(model);
    }

    [HttpPost("nuevo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticleDto model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesViewBagAsync();
            return View(model);
        }

        try
        {
            await _articleService.CreateAsync(model);
            TempData["Success"] = "Artículo publicado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadCategoriesViewBagAsync();
            return View(model);
        }
    }

    [HttpGet("editar/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article is null) return NotFound();

        await LoadCategoriesViewBagAsync();

        var model = ArticleMapper.ToEditDto(article);

        ViewBag.ArticleId = id;
        ViewBag.CoverImageUrl = null;
        if (!string.IsNullOrEmpty(article.CoverImageUrl))
        {
            ViewBag.CoverImageUrl = article.CoverImageUrl;
        }
        else if (!string.IsNullOrEmpty(article.CoverImageId))
        {
            var coverImg = await _imageService.GetByIdAsync(article.CoverImageId);
            ViewBag.CoverImageUrl = ImageMapper.GetBestUrl(coverImg);
        }

        if (string.IsNullOrEmpty(model.CoverImageUrl) && !string.IsNullOrEmpty(ViewBag.CoverImageUrl))
        {
            model.CoverImageUrl = ViewBag.CoverImageUrl;
        }

        ViewBag.LinkedImages = await _imageService.GetByArticleIdAsync(id);
        ViewBag.AvailableImages = await _imageService.GetAllAsync(0, 50);

        return View(model);
    }

    [HttpPost("editar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ArticleDto model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesViewBagAsync();
            ViewBag.ArticleId = id;
            ViewBag.CoverImageUrl = model.CoverImageUrl;
            ViewBag.LinkedImages = await _imageService.GetByArticleIdAsync(id);
            ViewBag.AvailableImages = await _imageService.GetAllAsync(0, 50);
            return View(model);
        }

        try
        {
            await _articleService.UpdateAsync(id, model);
            TempData["Success"] = "Artículo actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadCategoriesViewBagAsync();
            ViewBag.ArticleId = id;
            ViewBag.CoverImageUrl = model.CoverImageUrl;
            ViewBag.LinkedImages = await _imageService.GetByArticleIdAsync(id);
            ViewBag.AvailableImages = await _imageService.GetAllAsync(0, 50);
            return View(model);
        }
    }

    [HttpPost("eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _articleService.DeleteAsync(id);
        TempData["Success"] = "Artículo eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("upload-inline")]
    public async Task<IActionResult> UploadInline(IFormFile file, string? articleId = null)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No se ha proporcionado ningún archivo." });

        using var stream = file.OpenReadStream();
        var metadata = await _imageService.ProcessAndStoreDtoAsync(
            file.FileName, stream, file.FileName, articleId);

        var url = ImageMapper.GetBestUrl(metadata) ?? metadata.Variants.FirstOrDefault()?.PublicUrl;

        return Json(new { id = metadata.Id, url = url, alt = metadata.Alt });
    }

    private async Task LoadCategoriesViewBagAsync()
    {
        var all = await _categoryService.GetAllAsync();
        ViewBag.ParentCategories = all.Where(c => c.ParentId is null).ToList();
        ViewBag.SubCategories = all.Where(c => c.ParentId is not null).ToList();
    }
}
