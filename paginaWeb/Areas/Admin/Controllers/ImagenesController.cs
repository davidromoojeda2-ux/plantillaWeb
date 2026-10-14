using Aplication.Interfaces;
using Aplication.Mappers;
using Aplication.Services;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> Index(int page = 1)
    {
        var images = await _imageService.GetAllAsync((page - 1) * 30, 30);
        var articles = await _articleService.GetAllAdminAsync(1, 100);
        var articlesDict = articles.ToDictionary(a => a.Id, a => a.Title);

        foreach (var img in images)
        {
            if (!string.IsNullOrEmpty(img.ArticleId) && articlesDict.TryGetValue(img.ArticleId, out var title))
            {
                img.ArticleTitle = title;
            }
        }

        ViewBag.Articles = articles;
        return View(images);
    }

    [HttpPost("subir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string? alt, string? articleId, bool setAsCover = false)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Selecciona un archivo de imagen válido.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = file.OpenReadStream();
            var metadata = await _imageService.ProcessAndStoreDtoAsync(
                file.FileName, stream, alt ?? Path.GetFileNameWithoutExtension(file.FileName), articleId);

            if (!string.IsNullOrEmpty(articleId))
            {
                var article = await _articleService.GetByIdAsync(articleId);
                if (article != null)
                {
                    var editDto = ArticleMapper.ToEditDto(article);
                    if (!editDto.ImageIds.Contains(metadata.Id))
                        editDto.ImageIds.Add(metadata.Id);

                    if (setAsCover)
                    {
                        editDto.CoverImageId = metadata.Id;
                        editDto.CoverImageUrl = ImageMapper.GetBestUrl(metadata);
                    }

                    await _articleService.UpdateAsync(articleId, editDto);
                }
            }

            TempData["Success"] = "Imagen procesada, almacenada" + (!string.IsNullOrEmpty(articleId) ? " y vinculada al artículo" : "") + " correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al procesar la imagen: " + ex.Message;
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
                var article = await _articleService.GetByIdAsync(articleId);
                if (article != null)
                {
                    var editDto = ArticleMapper.ToEditDto(article);
                    if (!editDto.ImageIds.Contains(id))
                        editDto.ImageIds.Add(id);

                    if (setAsCover)
                    {
                        editDto.CoverImageId = id;
                        editDto.CoverImageUrl = ImageMapper.GetBestUrl(img);
                    }

                    await _articleService.UpdateAsync(articleId, editDto);
                    TempData["Success"] = $"Imagen vinculada exitosamente con '{article.Title}'" + (setAsCover ? " y asignada como portada." : ".");
                    return RedirectToAction(nameof(Index));
                }
            }

            TempData["Success"] = "Vínculo de la imagen actualizado correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al vincular imagen: " + ex.Message;
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
        await _imageService.DeleteByIdAsync(id);
        TempData["Success"] = "Imagen y sus variantes WebP eliminadas.";
        return RedirectToAction(nameof(Index));
    }
}
