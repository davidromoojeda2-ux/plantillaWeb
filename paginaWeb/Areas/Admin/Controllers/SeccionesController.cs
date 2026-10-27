using Aplication.DTOs.ContentBlocks;
using Aplication.Exceptions;
using Aplication.Services;
using Domine.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using paginaWeb.Areas.Admin.Models;
using paginaWeb.Helpers;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/secciones")]
public class SeccionesController : Controller
{
    private readonly IPageService _pageService;

    public SeccionesController(IPageService pageService)
    {
        _pageService = pageService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var pages = await _pageService.GetAllAsync();
        return View(pages);
    }

    [HttpGet("nueva")]
    public IActionResult Create()
    {
        return View(new PageFormViewModel { Order = 1, ShowInFooter = true });
    }

    [HttpPost("nueva")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PageFormViewModel model)
    {
        ContentBlockFormHelper.TryParse(model.BlocksJson, ModelState, out var blocks);

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _pageService.CreateAsync(model.ToDto(blocks));
            TempData["Success"] = "Página informativa creada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            return View(model);
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet("editar/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var page = await _pageService.GetByIdAsync(id);
        if (page is null) return NotFound();

        var model = PageFormViewModel.FromDto(page);

        ViewBag.PageId = id;
        return View(model);
    }

    [HttpPost("editar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, PageFormViewModel model)
    {
        ContentBlockFormHelper.TryParse(model.BlocksJson, ModelState, out var blocks);

        if (!ModelState.IsValid)
        {
            ViewBag.PageId = id;
            return View(model);
        }

        try
        {
            await _pageService.UpdateAsync(id, model.ToDto(blocks));
            TempData["Success"] = "Página informativa actualizada.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            ViewBag.PageId = id;
            return View(model);
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            ViewBag.PageId = id;
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
            await _pageService.DeleteAsync(id);
            TempData["Success"] = "Página informativa eliminada.";
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}
