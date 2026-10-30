using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using paginaWeb.Areas.Admin.Models;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/automatizacion")]
public class AutomationController : Controller
{
    private readonly IAutomationService _automation;
    private readonly AutomationCoordinator _coordinator;
    private readonly ICategoryService _categoryService;

    public AutomationController(
        IAutomationService automation,
        AutomationCoordinator coordinator,
        ICategoryService categoryService)
    {
        _automation = automation;
        _coordinator = coordinator;
        _categoryService = categoryService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var model = new AutomationIndexViewModel
        {
            Status = await _automation.GetStatusAsync(),
            RecentRuns = await _automation.GetRunsAsync(0, 10),
            PendingDecisions = await _automation.GetPendingDecisionsAsync()
        };
        return View(model);
    }

    [HttpPost("run")]
    [ValidateAntiForgeryToken]
    public IActionResult Run()
    {
        _coordinator.SignalRun();
        TempData["Success"] = "Corrida iniciada. El estado se actualiza en la barra de progreso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("estado")]
    public async Task<IActionResult> Estado()
    {
        var status = await _automation.GetStatusAsync();
        return Json(new { running = status.IsRunning, lastRun = status.LastRun });
    }

    [HttpPost("toggle-pausa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePause()
    {
        var settings = await _automation.GetSettingsAsync();
        settings.Paused = !settings.Paused;
        await _automation.SaveSettingsAsync(settings);
        _coordinator.SetPaused(settings.Paused);

        TempData["Success"] = settings.Paused
            ? "Automatización pausada. Las corridas programadas quedan detenidas."
            : "Automatización reanudada.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("historial")]
    public async Task<IActionResult> Historial(int page = 1)
    {
        const int pageSize = 20;
        var runs = await _automation.GetRunsAsync((page - 1) * pageSize, pageSize);
        ViewBag.Page = page;
        return View(runs);
    }

    [HttpGet("detalle/{id}")]
    public async Task<IActionResult> Detalle(string id)
    {
        var run = await _automation.GetRunAsync(id);
        if (run is null) return NotFound();
        return View(run);
    }

    [HttpGet("configuracion")]
    public async Task<IActionResult> Configuracion()
    {
        var settings = await _automation.GetSettingsAsync();
        var categories = await _categoryService.GetAllAsync();
        var model = AutomationConfigViewModel.FromSettings(settings, categories);

        return View(model);
    }

    [HttpPost("configuracion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Configuracion(AutomationConfigViewModel model, bool exit = false)
    {
        var existing = await _automation.GetSettingsAsync();

        if (exit)
        {
            var errors = new List<string>();

            if (model.Enabled)
            {
                if (string.IsNullOrWhiteSpace(model.SerperApiKey) && string.IsNullOrWhiteSpace(existing.SerperApiKey))
                    errors.Add("Falta la clave de Serper.");
                if (string.IsNullOrWhiteSpace(model.AiApiKey) && string.IsNullOrWhiteSpace(existing.AiApiKey))
                    errors.Add("Falta la clave de IA.");
                if (string.IsNullOrWhiteSpace(model.BaseTopicsText))
                    errors.Add("Debes indicar al menos una temática base.");
                if (model.CategoryIds is null || model.CategoryIds.Count == 0)
                    errors.Add("Debes seleccionar al menos una categoría.");
            }

            if (errors.Count > 0)
            {
                TempData["Error"] = "Configuración incompleta: " + string.Join(" ", errors);
                model.Categories = await _categoryService.GetAllAsync();
                model.HasSerperKey = !string.IsNullOrWhiteSpace(model.SerperApiKey) || !string.IsNullOrWhiteSpace(existing.SerperApiKey);
                model.HasAiKey = !string.IsNullOrWhiteSpace(model.AiApiKey) || !string.IsNullOrWhiteSpace(existing.AiApiKey);
                return View(model);
            }
        }

        var settings = model.ToSettings(existing);
        await _automation.SaveSettingsAsync(settings);
        _coordinator.SetPaused(settings.Paused);

        TempData["Success"] = exit ? "Configuración validada correctamente." : "Configuración guardada.";
        return exit ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Configuracion));
    }

    [HttpPost("crear-categoria")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCategoria(string name, string? parentId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "El nombre de la categoría es obligatorio.";
            return RedirectToAction(nameof(Configuracion));
        }

        try
        {
            await _categoryService.CreateAsync(new CategoryDto
            {
                Name = name.Trim(),
                Slug = Aplication.Services.SlugGenerator.Slugify(name),
                ParentId = parentId,
                ShowInMenu = true,
                IsActive = true
            });
            TempData["Success"] = "Categoría creada correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Configuracion));
    }

    [HttpPost("decision/{runId}/{decisionId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveDecision(string runId, string decisionId, bool continuar, DateTime? scheduledAt = null)
    {
        try
        {
            await _automation.ResolveDecisionAsync(runId, decisionId, continuar, scheduledAt);
            TempData["Success"] = continuar
                ? "Artículo generado a partir de la temática."
                : "Temática omitida.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
