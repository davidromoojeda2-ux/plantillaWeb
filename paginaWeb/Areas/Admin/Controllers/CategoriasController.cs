using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/categorias")]
public class CategoriasController : Controller
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly IMenuService _menuService;

    public CategoriasController(ICategoryRepository categoryRepo, IMenuService menuService)
    {
        _categoryRepo = categoryRepo;
        _menuService = menuService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var all = await _categoryRepo.GetAllAsync();
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
        if (await _categoryRepo.SlugExistsAsync(model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), $"El slug '{model.Slug}' ya está registrado.");
        }

        if (!ModelState.IsValid)
        {
            await LoadParentsViewBagAsync();
            return View(model);
        }

        var category = new Category
        {
            Name = model.Name,
            Slug = model.Slug.ToLowerInvariant(),
            Description = model.Description,
            ParentId = string.IsNullOrEmpty(model.ParentId) ? null : model.ParentId,
            Order = model.Order,
            ShowInMenu = model.ShowInMenu,
            IsActive = model.IsActive
        };

        await _categoryRepo.InsertAsync(category);
        _menuService.InvalidateMenu();

        TempData["Success"] = "Categoría creada con éxito e invalidada la caché del menú.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("editar/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category is null) return NotFound();

        await LoadParentsViewBagAsync(id);

        var model = new CategoryDto
        {
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            Order = category.Order,
            ShowInMenu = category.ShowInMenu,
            IsActive = category.IsActive
        };

        ViewBag.CategoryId = id;
        return View(model);
    }

    [HttpPost("editar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, CategoryDto model)
    {
        if (await _categoryRepo.SlugExistsAsync(model.Slug, id))
        {
            ModelState.AddModelError(nameof(model.Slug), $"El slug '{model.Slug}' ya está en uso.");
        }

        if (!ModelState.IsValid)
        {
            await LoadParentsViewBagAsync(id);
            ViewBag.CategoryId = id;
            return View(model);
        }

        var category = await _categoryRepo.GetByIdAsync(id);
        if (category is null) return NotFound();

        category.Name = model.Name;
        category.Slug = model.Slug.ToLowerInvariant();
        category.Description = model.Description;
        category.ParentId = string.IsNullOrEmpty(model.ParentId) ? null : model.ParentId;
        category.Order = model.Order;
        category.ShowInMenu = model.ShowInMenu;
        category.IsActive = model.IsActive;

        await _categoryRepo.UpdateAsync(category);
        _menuService.InvalidateMenu();

        TempData["Success"] = "Categoría actualizada e invalidada la caché del menú.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _categoryRepo.DeleteAsync(id);
        _menuService.InvalidateMenu();
        TempData["Success"] = "Categoría eliminada con éxito.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadParentsViewBagAsync(string? excludeId = null)
    {
        var all = await _categoryRepo.GetAllAsync();
        var parents = all.Where(c => c.ParentId is null && (excludeId == null || c.Id != excludeId)).ToList();
        ViewBag.Parents = parents;
    }
}
