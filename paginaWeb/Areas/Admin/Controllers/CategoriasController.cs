using Aplication.DTOs;
using Aplication.Exceptions;
using Aplication.Mappers;
using Aplication.Services;
using Domine.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using paginaWeb.Areas.Admin.Models;

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
        var parents = await GetParentsAsync();
        var model = new CategoryFormViewModel
        {
            Order = 1,
            ShowInMenu = true,
            IsActive = true,
            Parents = parents
        };
        return View(model);
    }

    [HttpPost("nueva")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Parents = await GetParentsAsync();
            return View(model);
        }

        try
        {
            await _categoryService.CreateAsync(model.ToDto());
            _menuService.InvalidateMenu();

            TempData["Success"] = "Categoría creada con éxito e invalidada la caché del menú.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            model.Parents = await GetParentsAsync();
            return View(model);
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            model.Parents = await GetParentsAsync();
            return View(model);
        }
    }

    [HttpGet("editar/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category is null) return NotFound();

        var parents = await GetParentsAsync(id);
        var model = CategoryFormViewModel.FromDto(category, parents);

        return View(model);
    }

    [HttpPost("editar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, CategoryFormViewModel model)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            model.Parents = await GetParentsAsync(id);
            return View(model);
        }

        try
        {
            await _categoryService.UpdateAsync(id, model.ToDto());
            _menuService.InvalidateMenu();

            TempData["Success"] = "Categoría actualizada e invalidada la caché del menú.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            model.Parents = await GetParentsAsync(id);
            return View(model);
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            model.Parents = await GetParentsAsync(id);
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
            await _categoryService.DeleteAsync(id);
            _menuService.InvalidateMenu();
            TempData["Success"] = "Categoría eliminada con éxito.";
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<List<CategoryResponseDto>> GetParentsAsync(string? excludeId = null)
    {
        var all = await _categoryService.GetAllAsync();
        return all
            .Where(c => c.ParentId is null && (excludeId is null || c.Id != excludeId))
            .OrderBy(c => c.Order)
            .ToList();
    }
}
