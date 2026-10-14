using Aplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/comentarios")]
public class ComentariosController : Controller
{
    private readonly ICommentService _commentService;

    public ComentariosController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? estado = null)
    {
        var comments = await _commentService.GetByStatusAsync(estado);
        ViewBag.Estado = estado ?? "pendiente";
        return View(comments);
    }

    [HttpPost("aprobar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id, string? returnEstado = null)
    {
        await _commentService.ApproveAsync(id);
        TempData["Success"] = "Comentario aprobado; ahora es visible en el portal público.";
        return RedirectToAction(nameof(Index), new { estado = returnEstado });
    }

    [HttpPost("rechazar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(string id, string? returnEstado = null)
    {
        await _commentService.RejectAsync(id);
        TempData["Success"] = "Comentario rechazado.";
        return RedirectToAction(nameof(Index), new { estado = returnEstado });
    }

    [HttpPost("eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, string? returnEstado = null)
    {
        await _commentService.DeleteAsync(id);
        TempData["Success"] = "Comentario eliminado definitivamente.";
        return RedirectToAction(nameof(Index), new { estado = returnEstado });
    }
}
