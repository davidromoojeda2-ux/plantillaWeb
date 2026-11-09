using Aplication.DTOs;
using Aplication.Exceptions;
using Aplication.Mappers;
using Aplication.Services;
using Domine.Entities;
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
    public async Task<IActionResult> Create([FromQuery] string? parentId = null)
    {
        var parents = await GetParentsAsync();
        var model = new CategoryFormViewModel
        {
            Order = 1,
            ShowInMenu = true,
            IsActive = true,
            ParentId = parentId,
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
            var all = await _categoryService.GetAllAsync();
            var subs = all.Where(s => s.ParentId == id).ToList();
            foreach (var sub in subs)
            {
                await _categoryService.DeleteAsync(sub.Id);
            }

            await _categoryService.DeleteAsync(id);
            _menuService.InvalidateMenu();

            TempData["Success"] = subs.Count > 0
                ? $"Categoría principal y sus {subs.Count} subcategorías asociadas fueron eliminadas con éxito."
                : "Categoría eliminada con éxito.";
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, string? excludeId = null)
    {
        var baseSlug = SlugGenerator.Slugify(name);
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "categoria";

        var slug = baseSlug;
        var count = 1;
        while (await _categoryService.SlugExistsAsync(slug, excludeId))
        {
            slug = $"{baseSlug}-{count++}";
        }

        return slug;
    }

    private static object ToJson(CategoryResponseDto category) => new
    {
        id = category.Id,
        parentId = category.ParentId,
        name = category.Name,
        slug = category.Slug,
        description = category.Description,
        order = category.Order,
        showInMenu = category.ShowInMenu,
        isActive = category.IsActive
    };

    private async Task<List<CategoryResponseDto>> GetParentsAsync(string? excludeId = null)
    {
        var all = await _categoryService.GetAllAsync();
        return all
            .Where(c => c.ParentId is null && (excludeId is null || c.Id != excludeId))
            .OrderBy(c => c.Order)
            .ToList();
    }

    /// <summary>
    /// Árbol completo de categorías consumido por el modal reutilizable de gestión.
    /// </summary>
    [HttpGet("listado")]
    public async Task<IActionResult> List()
    {
        var categories = await _categoryService.GetAllAsync();
        return Json(categories.Select(ToJson));
    }

    /// <summary>
    /// Crea o actualiza una categoría desde el modal de gestión.
    /// </summary>
    [HttpPost("guardar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromBody] CategorySaveRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { success = false, message = "El nombre de la categoría es obligatorio." });
        }

        var name = request.Name.Trim();
        var parentId = string.IsNullOrWhiteSpace(request.ParentId) ? null : request.ParentId.Trim();

        try
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                var slug = await GenerateUniqueSlugAsync(name);
                var created = await _categoryService.CreateAsync(new CategoryDto
                {
                    Name = name,
                    Slug = slug,
                    ParentId = parentId,
                    Description = request.Description?.Trim(),
                    Order = request.Order,
                    ShowInMenu = request.ShowInMenu,
                    IsActive = request.IsActive
                });

                _menuService.InvalidateMenu();
                return Ok(new { success = true, category = ToJson(created) });
            }

            var all = await _categoryService.GetAllAsync();
            var existing = all.FirstOrDefault(c => c.Id == request.Id);
            if (existing is null)
            {
                return NotFound(new { success = false, message = "La categoría no existe." });
            }

            if (!string.IsNullOrEmpty(parentId))
            {
                if (string.Equals(parentId, request.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "Una categoría no puede ser padre de sí misma." });
                }

                if (all.Any(c => c.ParentId == request.Id))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No puedes convertir en subcategoría una categoría que ya tiene subcategorías."
                    });
                }
            }

            var slugUpdated = await GenerateUniqueSlugAsync(name, request.Id);
            var updated = await _categoryService.UpdateAsync(request.Id, new CategoryDto
            {
                Name = name,
                Slug = slugUpdated,
                ParentId = parentId,
                Description = request.Description?.Trim(),
                Order = request.Order,
                ShowInMenu = request.ShowInMenu,
                IsActive = request.IsActive
            });

            _menuService.InvalidateMenu();
            return Ok(new { success = true, category = ToJson(updated) });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina una categoría (y opcionalmente sus subcategorías) desde el modal de gestión.
    /// </summary>
    [HttpPost("eliminar-ajax/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAjax(string id, bool deleteChildren = true)
    {
        try
        {
            var all = await _categoryService.GetAllAsync();
            var category = all.FirstOrDefault(c => c.Id == id);
            if (category is null)
            {
                return NotFound(new { success = false, message = "La categoría no existe." });
            }

            var children = all.Where(c => c.ParentId == id).ToList();
            if (children.Count > 0 && !deleteChildren)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "La categoría tiene subcategorías asociadas. Elimínalas primero."
                });
            }

            foreach (var child in children)
            {
                await _categoryService.DeleteAsync(child.Id);
            }

            await _categoryService.DeleteAsync(id);
            _menuService.InvalidateMenu();

            return Ok(new { success = true, deletedChildren = children.Count });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("crear-rapida")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreate([FromBody] QuickCategoryRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { success = false, message = "El nombre de la categoría es obligatorio." });

        var trimmedName = request.Name.Trim();
        var slug = trimmedName.ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .Aggregate(new System.Text.StringBuilder(), (sb, c) => sb.Append(c))
            .ToString();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9]+", "-").Trim('-');
        var baseSlug = string.IsNullOrEmpty(slug) ? "categoria" : slug;

        var finalSlug = baseSlug;
        var count = 1;
        while (await _categoryService.SlugExistsAsync(finalSlug))
        {
            finalSlug = $"{baseSlug}-{count++}";
        }

        var dto = new CategoryDto
        {
            Name = trimmedName,
            Slug = finalSlug,
            ParentId = string.IsNullOrWhiteSpace(request.ParentId) ? null : request.ParentId.Trim(),
            Description = request.Description?.Trim(),
            Order = 99,
            ShowInMenu = true,
            IsActive = true
        };

        try
        {
            var created = await _categoryService.CreateAsync(dto);
            _menuService.InvalidateMenu();

            return Ok(new
            {
                success = true,
                id = created.Id,
                name = created.Name,
                slug = created.Slug,
                parentId = created.ParentId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class QuickCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? Description { get; set; }
}

public class CategorySaveRequest
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool ShowInMenu { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
