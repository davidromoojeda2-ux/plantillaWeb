using Aplication.DTOs;
using Aplication.Exceptions;
using Aplication.Interfaces;
using Aplication.Mappers;
using Aplication.Services;
using Domine.Exceptions;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using paginaWeb.Areas.Admin.Models;

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
        var allCategories = await _categoryService.GetAllAsync();
        var model = new ArticleFormViewModel
        {
            Author = User.Identity?.Name ?? "Redacción",
            Status = "Publicado",
            ParentCategories = allCategories.Where(c => c.ParentId is null).OrderBy(c => c.Order).ToList(),
            SubCategories = allCategories.Where(c => c.ParentId is not null).OrderBy(c => c.Order).ToList()
        };
        return View(model);
    }

    [HttpPost("nuevo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateTaxonomyAsync(model);
            return View(model);
        }

        try
        {
            await _articleService.CreateAsync(model.ToDto());
            TempData["Success"] = "Artículo publicado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            await PopulateTaxonomyAsync(model);
            return View(model);
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            await PopulateTaxonomyAsync(model);
            return View(model);
        }
    }

    [HttpGet("editar/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article is null) return NotFound();

        var allCategories = await _categoryService.GetAllAsync();
        var parents = allCategories.Where(c => c.ParentId is null).OrderBy(c => c.Order).ToList();
        var subs = allCategories.Where(c => c.ParentId is not null).OrderBy(c => c.Order).ToList();

        string? coverUrl = article.CoverImageUrl;
        if (string.IsNullOrEmpty(coverUrl) && !string.IsNullOrEmpty(article.CoverImageId))
        {
            var coverImg = await _imageService.GetByIdAsync(article.CoverImageId);
            coverUrl = ImageMapper.GetBestUrl(coverImg);
        }

        var linkedImages = await _imageService.GetByArticleIdAsync(id);
        var availableImages = await _imageService.GetAllAsync(0, 50);

        var model = ArticleFormViewModel.FromDto(
            article, parents, subs, coverUrl, linkedImages, availableImages);

        return View(model);
    }

    [HttpPost("editar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ArticleFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            await PopulateCollectionsAsync(model, id);
            return View(model);
        }

        try
        {
            await _articleService.UpdateAsync(id, model.ToDto());
            TempData["Success"] = "Artículo actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            await PopulateCollectionsAsync(model, id);
            return View(model);
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            await PopulateCollectionsAsync(model, id);
            return View(model);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _articleService.DeleteAsync(id);
            TempData["Success"] = "Artículo eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
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

    private async Task PopulateTaxonomyAsync(ArticleFormViewModel model)
    {
        var allCategories = await _categoryService.GetAllAsync();
        model.ParentCategories = allCategories.Where(c => c.ParentId is null).OrderBy(c => c.Order).ToList();
        model.SubCategories = allCategories.Where(c => c.ParentId is not null).OrderBy(c => c.Order).ToList();
    }

    private async Task PopulateCollectionsAsync(ArticleFormViewModel model, string id)
    {
        await PopulateTaxonomyAsync(model);
        model.LinkedImages = await _imageService.GetByArticleIdAsync(id);
        model.AvailableImages = await _imageService.GetAllAsync(0, 50);
    }
}
