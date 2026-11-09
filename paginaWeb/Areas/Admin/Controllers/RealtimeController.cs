using Aplication.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.Areas.Admin.Controllers;

/// <summary>
/// Endpoint mínimo para el polling de respaldo. Devuelve solo versiones en memoria;
/// no consulta MongoDB ni expone datos del panel.
/// </summary>
[Area("Admin")]
[Authorize]
[Route("admin/changes")]
public sealed class RealtimeController : Controller
{
    private readonly ICmsChangePublisher _changes;

    public RealtimeController(ICmsChangePublisher changes) => _changes = changes;

    [HttpGet("")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Get()
    {
        var versions = _changes.GetVersions();

        return Json(new
        {
            automation = versions[CmsModule.Automation],
            articles = versions[CmsModule.Articles],
            categories = versions[CmsModule.Categories],
            comments = versions[CmsModule.Comments],
            timestamp = DateTime.UtcNow
        });
    }
}
