using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
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
    private readonly ICategoryRepository _categoryRepo;
    private readonly IImageMetadataRepository _imageRepo;
    private readonly ImageService _imageService;

    public ArticulosController(
        IArticleService articleService,
        ICategoryRepository categoryRepo,
        IImageMetadataRepository imageRepo,
        ImageService imageService)
    {
        _articleService = articleService;
        _categoryRepo = categoryRepo;
        _imageRepo = imageRepo;
        _imageService = imageService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var articles = await _articleService.GetAllAdminAsync(page, 20);
        var categories = await _categoryRepo.GetAllAsync();
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

        var model = new ArticleDto
        {
            Title = article.Title,
            Slug = article.Slug,
            Summary = article.Summary,
            Content = article.Content,
            Author = article.Author,
            CategoryId = article.CategoryId,
            SubcategoryId = article.SubcategoryId,
            Tags = article.Tags,
            CoverImageId = article.CoverImageId,
            Featured = article.Featured,
            Status = article.Status.ToString()
        };

        ViewBag.ArticleId = id;
        ViewBag.CoverImageUrl = null;
        if (!string.IsNullOrEmpty(article.CoverImageId))
        {
            var coverImg = await _imageRepo.GetByIdAsync(article.CoverImageId);
            ViewBag.CoverImageUrl = coverImg?.Variants.OrderByDescending(v => v.Width).FirstOrDefault()?.PublicUrl;
        }

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
    public async Task<IActionResult> UploadInline(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No se ha proporcionado ningún archivo." });

        using var stream = file.OpenReadStream();
        var metadata = await _imageService.ProcessAndStoreAsync(
            file.FileName, stream, file.FileName);

        var url = metadata.Variants.OrderByDescending(v => v.Width).FirstOrDefault()?.PublicUrl
               ?? metadata.Variants.First().PublicUrl;

        return Json(new { id = metadata.Id, url = url, alt = metadata.Alt });
    }

    private async Task LoadCategoriesViewBagAsync()
    {
        var all = await _categoryRepo.GetAllAsync();
        ViewBag.ParentCategories = all.Where(c => c.ParentId is null).ToList();
        ViewBag.SubCategories = all.Where(c => c.ParentId is not null).ToList();
    }
}
