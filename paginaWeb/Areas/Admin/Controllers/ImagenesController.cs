using Aplication.Interfaces;
using Domine.Entities;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/imagenes")]
public class ImagenesController : Controller
{
    private readonly IImageMetadataRepository _imageRepo;
    private readonly ImageService _imageService;

    public ImagenesController(IImageMetadataRepository imageRepo, ImageService imageService)
    {
        _imageRepo = imageRepo;
        _imageService = imageService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var images = await _imageRepo.GetAllAsync((page - 1) * 30, 30);
        return View(images);
    }

    [HttpPost("subir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string? alt)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Selecciona un archivo de imagen válido.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = file.OpenReadStream();
            await _imageService.ProcessAndStoreAsync(
                file.FileName, stream, alt ?? Path.GetFileNameWithoutExtension(file.FileName));

            TempData["Success"] = "Imagen procesada a WebP y almacenada correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al procesar la imagen: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("actualizar-alt")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAlt(string id, string alt)
    {
        await _imageRepo.UpdateAltAsync(id, alt ?? "");
        TempData["Success"] = "Texto alternativo actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var metadata = await _imageRepo.GetByIdAsync(id);
        if (metadata != null)
        {
            await _imageService.DeleteAsync(metadata);
            TempData["Success"] = "Imagen y sus variantes WebP eliminadas.";
        }
        return RedirectToAction(nameof(Index));
    }
}
