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

    [HttpPost("vincular-articulo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkArticle(string id, string? articleId, bool setAsCover = false)
    {
        try
        {
            var img = await _imageService.GetByIdAsync(id);
            if (img == null)
            {
                TempData["Error"] = "Imagen no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            var previousArticleId = img.ArticleId;

            // Actualizar el vínculo en la colección de imágenes
            await _imageService.UpdateArticleIdAsync(id, articleId);

            // Si estaba vinculada a otro artículo anteriormente, desvincular de su ImageIds
            if (!string.IsNullOrEmpty(previousArticleId) && previousArticleId != articleId)
            {
                var prevArt = await _articleService.GetByIdAsync(previousArticleId);
                if (prevArt != null && prevArt.ImageIds.Contains(id))
                {
                    var prevDto = ArticleMapper.ToEditDto(prevArt);
                    prevDto.ImageIds.Remove(id);
                    await _articleService.UpdateAsync(previousArticleId, prevDto);
                }
            }

            // Si se asigna a un nuevo artículo
            if (!string.IsNullOrEmpty(articleId))
            {
                var linked = await LinkImageToArticleAsync(articleId, id, ImageMapper.GetBestUrl(img), setAsCover);
                if (linked)
                {
                    var article = await _articleService.GetByIdAsync(articleId);
                    TempData["Success"] = $"Imagen vinculada exitosamente con '{article?.Title}'" + (setAsCover ? " y asignada como portada." : ".");
                    return RedirectToAction(nameof(Index));
                }
            }

            TempData["Success"] = "Vínculo de la imagen actualizado correctamente.";
        }
        catch (DomainValidationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (ConflictException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (NotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("actualizar-alt")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAlt(string id, string alt)
    {
        await _imageService.UpdateAltAsync(id, alt ?? "");
        TempData["Success"] = "Texto alternativo actualizado.";
        return RedirectToAction(nameof(Index));
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
