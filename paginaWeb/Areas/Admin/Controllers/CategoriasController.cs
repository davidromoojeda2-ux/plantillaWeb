using Aplication.DTOs;
using Aplication.Mappers;
using Aplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/categorias")]
public class CategoriasController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IMenuService _menuService;

    public CategoriasController(ICategoryService categoryService, IMenuService menuService)
    {
        _categoryService = categoryService;
        _menuService = menuService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var all = await _categoryService.GetAllAsync();
        var parents = all.Where(c => c.ParentId is null).OrderBy(c => c.Order).ToList();
        var subs = all.Where(c => c.ParentId is not null).ToList();

        ViewBag.Subs = subs;
        return View(parents);
    }

    [HttpGet("nueva")]
    public async Task<IActionResult> Create()
    {
        await LoadParentsViewBagAsync();
        return View(new CategoryDto { Order = 1, ShowInMenu = true, IsActive = true });
    }

    [HttpPost("nueva")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryDto model)
    {
        if (await _categoryService.SlugExistsAsync(model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), $"El slug '{model.Slug}' ya está registrado.");
        }

        if (!ModelState.IsValid)
        {
            await LoadParentsViewBagAsync();
            return View(model);
        }

        await _categoryService.CreateAsync(model);
        _menuService.InvalidateMenu();

        TempData["Success"] = "Categoría creada con éxito e invalidada la caché del menú.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("editar/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category is null) return NotFound();

        await LoadParentsViewBagAsync(id);

        var model = CategoryMapper.ToEditDto(category);

        ViewBag.CategoryId = id;
        return View(model);
    }

    [HttpPost("editar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, CategoryDto model)
    {
        if (await _categoryService.SlugExistsAsync(model.Slug, id))
        {
            ModelState.AddModelError(nameof(model.Slug), $"El slug '{model.Slug}' ya está en uso.");
        }

        if (!ModelState.IsValid)
        {
            await LoadParentsViewBagAsync(id);
            ViewBag.CategoryId = id;
            return View(model);
        }

        await _categoryService.UpdateAsync(id, model);
        _menuService.InvalidateMenu();

        TempData["Success"] = "Categoría actualizada e invalidada la caché del menú.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _categoryService.DeleteAsync(id);
        _menuService.InvalidateMenu();
        TempData["Success"] = "Categoría eliminada con éxito.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadParentsViewBagAsync(string? excludeId = null)
    {
        var all = await _categoryService.GetAllAsync();
        ViewBag.Parents = all
            .Where(c => c.ParentId is null && (excludeId is null || c.Id != excludeId))
            .OrderBy(c => c.Order)
            .ToList();
    }
}
