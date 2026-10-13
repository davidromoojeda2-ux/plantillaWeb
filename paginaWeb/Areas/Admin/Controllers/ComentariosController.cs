using Aplication.Interfaces;
using Domine.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/comentarios")]
public class ComentariosController : Controller
{
    private readonly ICommentRepository _commentRepo;
    private readonly IArticleRepository _articleRepo;

    public ComentariosController(ICommentRepository commentRepo, IArticleRepository articleRepo)
    {
        _commentRepo = commentRepo;
        _articleRepo = articleRepo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? estado = null)
    {
        List<Comment> comments;
        if (estado == "aprobado")
            comments = await _commentRepo.GetByStatusAsync(CommentStatus.Aprobado);
        else if (estado == "rechazado")
            comments = await _commentRepo.GetByStatusAsync(CommentStatus.Rechazado);
        else if (estado == "todos")
            comments = await _commentRepo.GetAllAsync();
        else
            comments = await _commentRepo.GetPendingAsync();

        ViewBag.Estado = estado ?? "pendiente";
        return View(comments);
    }

    [HttpPost("aprobar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id, string? returnEstado = null)
    {
        await _commentRepo.UpdateStatusAsync(id, CommentStatus.Aprobado);
        TempData["Success"] = "Comentario aprobado; ahora es visible en el portal público.";
        return RedirectToAction(nameof(Index), new { estado = returnEstado });
    }

    [HttpPost("rechazar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(string id, string? returnEstado = null)
    {
        await _commentRepo.UpdateStatusAsync(id, CommentStatus.Rechazado);
        TempData["Success"] = "Comentario rechazado.";
        return RedirectToAction(nameof(Index), new { estado = returnEstado });
    }

    [HttpPost("eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, string? returnEstado = null)
    {
        await _commentRepo.DeleteAsync(id);
        TempData["Success"] = "Comentario eliminado definitivamente.";
        return RedirectToAction(nameof(Index), new { estado = returnEstado });
    }
}
