using Aplication.Content;
using Aplication.DTOs;
using Aplication.DTOs.ContentBlocks;
using Aplication.Exceptions;
using Aplication.Interfaces;
using Aplication.Mappers;
using Aplication.Services;
using Domine.Exceptions;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using paginaWeb.Areas.Admin.Models;
using paginaWeb.Helpers;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/articulos")]
public class ArticulosController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ImageService _imageService;
    private readonly IMenuService _menuService;

    public ArticulosController(
        IArticleService articleService,
        ICategoryService categoryService,
        ImageService imageService,
        IMenuService menuService)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _imageService = imageService;
        _menuService = menuService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var articles = await _articleService.GetAllAdminAsync(page, 50);
        var categories = await _categoryService.GetAllAsync();
        ViewBag.Categories = categories.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.Page = page;

        var coverIds = articles
            .Select(a => !string.IsNullOrEmpty(a.CoverImageId)
                ? a.CoverImageId
                : ContentImageResolver.FindFirstImage(a.Blocks)?.ImageId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct()
            .ToList();
        var images = await _imageService.GetVariantsDictAsync(coverIds);
        ViewBag.CoverImages = ImageMapper.ToThumbnailDictionary(images);

        return View(articles);
    }

    [HttpPost("toggle-destacado/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeatured(string id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article is null) return NotFound();

        var editDto = ArticleMapper.ToEditDto(article);
        bool willBeFeatured = !editDto.Featured;

        if (willBeFeatured)
        {
            var (isValid, errorMsg) = await ValidateHeroEligibilityAsync(
                editDto.CoverImageId, editDto.CoverImageUrl, editDto.Blocks, id);

            if (!isValid && errorMsg != null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = errorMsg });
                }
                TempData["Error"] = errorMsg;
                return RedirectToAction(nameof(Index));
            }
        }

        editDto.Featured = willBeFeatured;

        await _articleService.UpdateAsync(id, editDto);
        _menuService.InvalidateMenu();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
        {
            var allUpdated = await _articleService.GetAllAdminAsync(1, 100);
            int totalFeatured = allUpdated.Count(a => a.Featured);
            int activeFeatured = allUpdated.Count(a => a.Featured && string.Equals(a.Status, "Publicado", StringComparison.OrdinalIgnoreCase));
            return Json(new { success = true, featured = editDto.Featured, totalFeatured, activeFeatured });
        }

        TempData["Success"] = editDto.Featured
            ? "Artículo fijado como destacado en el Hero principal."
            : "Artículo removido de los destacados del Hero.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("nuevo")]
    public async Task<IActionResult> Create()
    {
        var allArticles = await _articleService.GetAllAdminAsync(1, 100);
        ViewBag.FeaturedCount = allArticles.Count(a => a.Featured);
        ViewBag.MaxHero = 3;

        var model = new ArticleFormViewModel
        {
            Author = User.Identity?.Name ?? "Redacción",
            Status = "Publicado"
        };
        await PopulateCollectionsAsync(model);
        return View(model);
    }

    [HttpPost("nuevo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticleFormViewModel model)
    {
        ContentBlockFormHelper.TryParse(model.BlocksJson, ModelState, out var blocks);
        await ValidateFeaturedHeroAsync(model, blocks);

        if (!ModelState.IsValid)
        {
            await PopulateCollectionsAsync(model);
            return View(model);
        }

        try
        {
            await _articleService.CreateAsync(model.ToDto(blocks));
            _menuService.InvalidateMenu();
            TempData["Success"] = "Artículo publicado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            await PopulateCollectionsAsync(model);
            return View(model);
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            await PopulateCollectionsAsync(model);
            return View(model);
        }
    }

    [HttpGet("editar/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article is null) return NotFound();

        var allArticles = await _articleService.GetAllAdminAsync(1, 100);
        ViewBag.FeaturedCount = allArticles.Count(a => a.Featured && a.Id != id);
        ViewBag.MaxHero = 3;

        string? coverUrl = await ResolveCoverImageUrlAsync(article);

        var model = ArticleFormViewModel.FromDto(article, coverUrl);
        await PopulateCollectionsAsync(model, id);

        return View(model);
    }


    [HttpPost("editar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ArticleFormViewModel model)
    {
        model.Id = id;
        ContentBlockFormHelper.TryParse(model.BlocksJson, ModelState, out var blocks);
        await ValidateFeaturedHeroAsync(model, blocks, id);

        if (!ModelState.IsValid)
        {
            await PopulateCollectionsAsync(model, id);
            return View(model);
        }

        try
        {
            await _articleService.UpdateAsync(id, model.ToDto(blocks));
            _menuService.InvalidateMenu();
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
            _menuService.InvalidateMenu();
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

    [HttpGet("imagenes-disponibles")]
    public async Task<IActionResult> GetAvailableImages(string? currentArticleId = null)
    {
        var images = await GetImagesWithArticleUsageAsync();
        var result = images.Select(img => new
        {
            id = img.Id,
            fileName = img.OriginalFileName,
            alt = img.Alt,
            width = img.OriginalWidth,
            height = img.OriginalHeight,
            uploadedAt = img.UploadedAt.ToString("dd/MM/yyyy"),
            url = ImageMapper.GetBestUrl(img) ?? img.Variants.FirstOrDefault()?.PublicUrl,
            thumbnailUrl = ImageMapper.GetThumbnailUrl(img) ?? ImageMapper.GetBestUrl(img),
            isUsed = !string.IsNullOrEmpty(img.ArticleId),
            usedInArticleId = img.ArticleId,
            usedInArticleTitle = img.ArticleTitle,
            isCurrentArticle = !string.IsNullOrEmpty(currentArticleId) && img.ArticleId == currentArticleId
        });

        return Json(result);
    }

    private async Task<List<ImageMetadataResponseDto>> GetImagesWithArticleUsageAsync()
    {
        var images = await _imageService.GetAllAsync(0, 100);
        var allArticles = await _articleService.GetAllAdminAsync(1, 200);

        var usageMap = new Dictionary<string, (string ArticleId, string ArticleTitle)>();

        foreach (var art in allArticles)
        {
            if (!string.IsNullOrEmpty(art.CoverImageId) && !usageMap.ContainsKey(art.CoverImageId))
            {
                usageMap[art.CoverImageId] = (art.Id, art.Title);
            }

            if (art.ImageIds != null)
            {
                foreach (var imgId in art.ImageIds)
                {
                    if (!string.IsNullOrEmpty(imgId) && !usageMap.ContainsKey(imgId))
                    {
                        usageMap[imgId] = (art.Id, art.Title);
                    }
                }
            }
        }

        var articlesDict = allArticles.ToDictionary(a => a.Id, a => a.Title);

        foreach (var img in images)
        {
            if (usageMap.TryGetValue(img.Id, out var usage))
            {
                img.ArticleId = usage.ArticleId;
                img.ArticleTitle = usage.ArticleTitle;
            }
            else if (!string.IsNullOrEmpty(img.ArticleId) && articlesDict.TryGetValue(img.ArticleId, out var title))
            {
                img.ArticleTitle = title;
            }
        }

        return images;
    }

    private async Task PopulateCollectionsAsync(ArticleFormViewModel model, string? id = null)
    {
        var allCategories = await _categoryService.GetAllAsync();
        model.ParentCategories = allCategories.Where(c => c.ParentId is null).OrderBy(c => c.Order).ToList();
        model.SubCategories = allCategories.Where(c => c.ParentId is not null).OrderBy(c => c.Order).ToList();
        model.LinkedImages = !string.IsNullOrEmpty(id) ? await _imageService.GetByArticleIdAsync(id) : [];
        model.AvailableImages = await GetImagesWithArticleUsageAsync();
    }

    private async Task<(bool IsValid, string? ErrorMessage)> ValidateHeroEligibilityAsync(
        string? coverImageId,
        string? coverImageUrl,
        IEnumerable<ContentBlockDto>? blocks,
        string? excludeArticleId = null)
    {
        bool hasImage = !string.IsNullOrEmpty(coverImageId)
                     || !string.IsNullOrWhiteSpace(coverImageUrl)
                     || ContentImageResolver.HasImage(blocks);

        if (!hasImage)
        {
            return (false, "No se puede designar en el Hero: El artículo debe contar con una imagen asignada.");
        }

        var allAdmin = await _articleService.GetAllAdminAsync(1, 100);
        int currentFeatured = allAdmin.Count(a => a.Featured && (excludeArticleId == null || a.Id != excludeArticleId));
        if (currentFeatured >= 3)
        {
            return (false, "Límite alcanzado: El Hero principal solo permite un máximo de 3 artículos destacados. Debes desmarcar uno antes de designar otro.");
        }

        return (true, null);
    }

    private async Task ValidateFeaturedHeroAsync(
        ArticleFormViewModel model,
        IEnumerable<ContentBlockDto>? blocks,
        string? excludeArticleId = null)
    {
        if (!model.Featured) return;

        var (isValid, errorMessage) = await ValidateHeroEligibilityAsync(
            model.CoverImageId, model.CoverImageUrl, blocks, excludeArticleId);

        if (!isValid && errorMessage != null)
        {
            ModelState.AddModelError("Featured", errorMessage);
        }
    }

    private async Task<string?> ResolveCoverImageUrlAsync(ArticleResponseDto article)
    {
        if (!string.IsNullOrEmpty(article.CoverImageUrl)) return article.CoverImageUrl;

        if (!string.IsNullOrEmpty(article.CoverImageId))
        {
            var coverImg = await _imageService.GetByIdAsync(article.CoverImageId);
            var bestUrl = ImageMapper.GetBestUrl(coverImg);
            if (!string.IsNullOrEmpty(bestUrl)) return bestUrl;
        }

        if (article.ImageIds.Count > 0)
        {
            var firstImg = await _imageService.GetByIdAsync(article.ImageIds.First());
            if (firstImg != null)
            {
                var bestUrl = ImageMapper.GetBestUrl(firstImg);
                if (!string.IsNullOrEmpty(bestUrl)) return bestUrl;
            }
        }

        var firstBodyImage = ContentImageResolver.FindFirstImage(article.Blocks);
        if (firstBodyImage is not null)
        {
            if (!string.IsNullOrWhiteSpace(firstBodyImage.ImageId))
            {
                var bodyImg = await _imageService.GetByIdAsync(firstBodyImage.ImageId);
                var bodyUrl = ImageMapper.GetBestUrl(bodyImg);
                if (!string.IsNullOrEmpty(bodyUrl)) return bodyUrl;
            }

            if (!string.IsNullOrWhiteSpace(firstBodyImage.Url))
                return firstBodyImage.Url;
        }

        return null;
    }
}

