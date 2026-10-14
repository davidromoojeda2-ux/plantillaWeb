using Aplication.DTOs;
using Aplication.Mappers;
using Aplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        return View(new PageDto { Order = 1, ShowInFooter = true });
    }

    [HttpPost("nueva")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PageDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _pageService.CreateAsync(model);
            TempData["Success"] = "Página informativa creada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet("editar/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var page = await _pageService.GetByIdAsync(id);
        if (page is null) return NotFound();

        var model = PageMapper.ToEditDto(page);

        ViewBag.PageId = id;
        return View(model);
    }

    [HttpPost("editar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, PageDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PageId = id;
            return View(model);
        }

        try
        {
            await _pageService.UpdateAsync(id, model);
            TempData["Success"] = "Página informativa actualizada.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.PageId = id;
            return View(model);
        }
    }

    [HttpPost("eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _pageService.DeleteAsync(id);
        TempData["Success"] = "Página informativa eliminada.";
        return RedirectToAction(nameof(Index));
    }
}
