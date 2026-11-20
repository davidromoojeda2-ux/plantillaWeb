using Aplication.DTOs;
using Aplication.Exceptions;
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
[Route("admin/imagenes")]
public class ImagenesController : Controller
{
    private readonly ImageService _imageService;
    private readonly IArticleService _articleService;
    private readonly ISiteSettingsService _siteSettingsService;

    public ImagenesController(
        ImageService imageService,
        IArticleService articleService,
        ISiteSettingsService siteSettingsService)
    {
        _imageService = imageService;
        _articleService = articleService;
        _siteSettingsService = siteSettingsService;
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

        ImageCatalogHelper.ApplyArticleUsage(images, articles);

        var storageStatus = await _imageService.GetStorageStatusAsync();
        var imageQuality = await _siteSettingsService.GetImageQualityAsync();

        var viewModel = new ImageLibraryViewModel
        {
            Images = images,
            Articles = articles,
            StorageStatus = storageStatus,
            ImageQuality = imageQuality,
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
    public async Task<IActionResult> List(string? currentArticleId = null)
    {
        var result = await ImageCatalogHelper.BuildAsync(_imageService, _articleService, currentArticleId);

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
            var metadata = await ProcessUploadAsync(file, customName, alt, articleId, setAsCover);

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
        catch (UnsupportedImageFormatException)
        {
            TempData["Error"] = "El archivo proporcionado no es un formato de imagen soportado (JPEG, PNG, WEBP, GIF, BMP, TIFF, AVIF o HEIC).";
        }
        catch (InvalidImageContentException)
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
            var metadata = await ProcessUploadAsync(file, customName, alt, articleId, setAsCover);
            var url = ImageMapper.GetBestUrl(metadata) ?? metadata.Variants.FirstOrDefault()?.PublicUrl;

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

    private async Task<ImageMetadataResponseDto> ProcessUploadAsync(
        IFormFile file,
        string? customName,
        string? alt,
        string? articleId,
        bool setAsCover)
    {
        var fileName = !string.IsNullOrWhiteSpace(customName) ? customName.Trim() : file.FileName.Trim();

        // Si el nombre personalizado no tiene extensión, conservar la del archivo subido.
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
            var url = ImageMapper.GetBestUrl(metadata) ?? metadata.Variants.FirstOrDefault()?.PublicUrl;
            await LinkImageToArticleAsync(articleId, metadata.Id, url, setAsCover);
        }

        return metadata;
    }

    private static string ResolveImageErrorMessage(Exception ex) => ex switch
    {
        ConflictException => ex.Message,
        DomainValidationException => ex.Message,
        NotFoundException => ex.Message,
        ArgumentException => "Error en el archivo de imagen: " + ex.Message,
        UnsupportedImageFormatException =>
            "El archivo proporcionado no es un formato de imagen soportado (JPEG, PNG, WEBP, GIF, BMP, TIFF, AVIF o HEIC).",
        InvalidImageContentException =>
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
