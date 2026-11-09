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
[Route("admin/imagenes")]
public class ImagenesController : Controller
{
    private readonly ImageService _imageService;
    private readonly IArticleService _articleService;

    public ImagenesController(ImageService imageService, IArticleService articleService)
    {
        _imageService = imageService;
        _articleService = articleService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        int page = 1,
        string? search = null,
        string? storage = null,
        string? articleId = null)
    {
        const int pageSize = 24;
        var (images, totalCount) = await _imageService.GetPagedAsync(page, pageSize, search, storage, articleId);
        var articles = await _articleService.GetAllAdminAsync(1, 100);
        var articlesDict = articles.ToDictionary(a => a.Id, a => a.Title);

        foreach (var img in images)
        {
            if (!string.IsNullOrEmpty(img.ArticleId) && articlesDict.TryGetValue(img.ArticleId, out var title))
            {
                img.ArticleTitle = title;
            }
        }

        var storageStatus = await _imageService.GetStorageStatusAsync();

        var viewModel = new ImageLibraryViewModel
        {
            Images = images,
            Articles = articles,
            StorageStatus = storageStatus,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = search,
            StorageFilter = storage,
            ArticleFilter = articleId
        };

        return View(viewModel);
    }

    /// <summary>
    /// Catálogo JSON consumido por el gestor de imágenes (modal reutilizable).
    /// </summary>
    [HttpGet("listado")]
    public async Task<IActionResult> List(string? search = null, string? currentArticleId = null)
    {
        var images = await GetImagesWithArticleUsageAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            images = images
                .Where(img =>
                    img.OriginalFileName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (img.Alt ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (img.ArticleTitle ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

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
            storage = img.Storage,
            isUsed = !string.IsNullOrEmpty(img.ArticleId),
            usedInArticleId = img.ArticleId,
            usedInArticleTitle = img.ArticleTitle,
            isCurrentArticle = !string.IsNullOrEmpty(currentArticleId) && img.ArticleId == currentArticleId
        });

        return Json(result);
    }

    [HttpGet("verificar-nombre")]
    public async Task<IActionResult> CheckName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Json(new { exists = false, available = true });
        }

        var exists = await _imageService.ExistsByNameAsync(fileName);
        return Json(new
        {
            exists = exists,
            available = !exists,
            fileName = fileName.Trim()
        });
    }

    [HttpPost("subir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        IFormFile file,
        string? customName,
        string? alt,
        string? articleId,
        bool setAsCover = false)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Selecciona un archivo de imagen válido.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var fileName = !string.IsNullOrWhiteSpace(customName) ? customName.Trim() : file.FileName.Trim();

            // Si el nombre personalizado no tiene extensión, conservar la del archivo subido
            if (!Path.HasExtension(fileName) && Path.HasExtension(file.FileName))
            {
                fileName += Path.GetExtension(file.FileName);
            }

            using var stream = file.OpenReadStream();
            var metadata = await _imageService.ProcessAndStoreDtoAsync(
                file.FileName,
                stream,
                alt ?? Path.GetFileNameWithoutExtension(fileName),
                articleId,
                fileName);

            if (!string.IsNullOrEmpty(articleId))
            {
                await LinkImageToArticleAsync(articleId, metadata.Id, ImageMapper.GetBestUrl(metadata), setAsCover);
            }

            var storageProviderName = metadata.Storage.Equals("r2", StringComparison.OrdinalIgnoreCase) ||
                                      metadata.Storage.Equals("s3", StringComparison.OrdinalIgnoreCase)
                ? "S3 / Cloudflare R2 (Prioritario)"
                : "Almacenamiento Local (Fallback)";

            TempData["Success"] = $"Imagen '{metadata.OriginalFileName}' optimizada con variantes WebP y guardada en {storageProviderName}." +
                                  (!string.IsNullOrEmpty(articleId) ? " Vinculada al artículo." : "");
        }
        catch (ConflictException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (DomainValidationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = "Error en el archivo de imagen: " + ex.Message;
        }
        catch (SixLabors.ImageSharp.UnknownImageFormatException)
        {
            TempData["Error"] = "El archivo proporcionado no es un formato de imagen soportado (use JPEG, PNG, WEBP, AVIF, etc.).";
        }
        catch (SixLabors.ImageSharp.InvalidImageContentException)
        {
            TempData["Error"] = "El archivo de imagen está dañado o su contenido no es válido.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error inesperado al procesar la imagen: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Subida desde el modal gestor de imágenes. Devuelve JSON para refrescar la galería sin recargar la página.
    /// </summary>
    [HttpPost("subir-ajax")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAjax(
        IFormFile file,
        string? customName,
        string? alt,
        string? articleId,
        bool setAsCover = false)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "Selecciona un archivo de imagen válido." });
        }

        try
        {
            var fileName = !string.IsNullOrWhiteSpace(customName) ? customName.Trim() : file.FileName.Trim();

            if (!Path.HasExtension(fileName) && Path.HasExtension(file.FileName))
            {
                fileName += Path.GetExtension(file.FileName);
            }

            using var stream = file.OpenReadStream();
            var metadata = await _imageService.ProcessAndStoreDtoAsync(
                file.FileName,
                stream,
                alt ?? Path.GetFileNameWithoutExtension(fileName),
                articleId,
                fileName);

            var url = ImageMapper.GetBestUrl(metadata) ?? metadata.Variants.FirstOrDefault()?.PublicUrl;

            if (!string.IsNullOrEmpty(articleId))
            {
                await LinkImageToArticleAsync(articleId, metadata.Id, url, setAsCover);
            }

            return Json(new
            {
                success = true,
                image = new
                {
                    id = metadata.Id,
                    fileName = metadata.OriginalFileName,
                    alt = metadata.Alt,
                    width = metadata.OriginalWidth,
                    height = metadata.OriginalHeight,
                    url,
                    thumbnailUrl = ImageMapper.GetThumbnailUrl(metadata) ?? url,
                    storage = metadata.Storage
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ResolveImageErrorMessage(ex) });
        }
    }

    [HttpPost("renombrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(string id, string fileName)
    {
        try
        {
            var updated = await _imageService.RenameAsync(id, fileName);
            return Json(new { success = true, fileName = updated.OriginalFileName });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("actualizar-alt")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAlt(string id, string alt)
    {
        await _imageService.UpdateAltAsync(id, alt ?? "");
        TempData["Success"] = "Texto alternativo actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("actualizar-alt-ajax")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAltAjax(string id, string alt)
    {
        try
        {
            await _imageService.UpdateAltAsync(id, alt ?? "");
            return Json(new { success = true, alt = alt ?? string.Empty });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _imageService.DeleteByIdAsync(id);
            TempData["Success"] = "La imagen y todas sus variantes WebP han sido eliminadas correctamente del almacenamiento y del catálogo.";
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al eliminar la imagen: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar-ajax/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAjax(string id)
    {
        try
        {
            await _imageService.DeleteByIdAsync(id);
            return Json(new { success = true });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Error al eliminar la imagen: " + ex.Message });
        }
    }

    private async Task<List<Aplication.DTOs.ImageMetadataResponseDto>> GetImagesWithArticleUsageAsync()
    {
        var images = await _imageService.GetAllAsync(0, 500);
        var allArticles = await _articleService.GetAllAdminAsync(1, 500);

        var usageMap = new Dictionary<string, (string ArticleId, string ArticleTitle)>();

        foreach (var art in allArticles)
        {
            if (!string.IsNullOrEmpty(art.CoverImageId) && !usageMap.ContainsKey(art.CoverImageId))
            {
                usageMap[art.CoverImageId] = (art.Id, art.Title);
            }

            if (art.ImageIds is not null)
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

    private static string ResolveImageErrorMessage(Exception ex) => ex switch
    {
        ConflictException => ex.Message,
        DomainValidationException => ex.Message,
        NotFoundException => ex.Message,
        ArgumentException => "Error en el archivo de imagen: " + ex.Message,
        SixLabors.ImageSharp.UnknownImageFormatException =>
            "El archivo proporcionado no es un formato de imagen soportado (use JPEG, PNG, WEBP, AVIF, etc.).",
        SixLabors.ImageSharp.InvalidImageContentException =>
            "El archivo de imagen está dañado o su contenido no es válido.",
        _ => "Error inesperado al procesar la imagen: " + ex.Message
    };

    private async Task<bool> LinkImageToArticleAsync(string? articleId, string imageId, string? coverUrl, bool setAsCover)
    {
        if (string.IsNullOrEmpty(articleId)) return false;

        var article = await _articleService.GetByIdAsync(articleId);
        if (article == null) return false;

        var editDto = ArticleMapper.ToEditDto(article);
        if (!editDto.ImageIds.Contains(imageId))
        {
            editDto.ImageIds.Add(imageId);
        }

        if (setAsCover)
        {
            editDto.CoverImageId = imageId;
            editDto.CoverImageUrl = coverUrl;
        }

        await _articleService.UpdateAsync(articleId, editDto);
        return true;
    }
}
