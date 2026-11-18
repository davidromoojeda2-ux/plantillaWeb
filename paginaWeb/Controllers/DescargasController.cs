using Aplication.Interfaces;
using Microsoft.AspNetCore.Mvc;
using paginaWeb.Helpers;

namespace paginaWeb.Controllers;

/// <summary>
/// Descarga pública de documentos. La autorización se decide en el servicio:
/// un documento es público si está marcado como tal o si pertenece a un
/// artículo publicado.
/// </summary>
[Route("documentos/descargar")]
public class DescargasController : Controller
{
    private readonly IDocumentService _documentService;

    public DescargasController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet("{id}")]
    [HttpGet("{id}/{slug}")]
    public async Task<IActionResult> Download(string id, string? slug, CancellationToken ct)
    {
        var document = await _documentService.GetByIdAsync(id, ct);
        if (document is null) return NotFound();

        if (!await _documentService.CanDownloadPublicAsync(id, ct))
            return NotFound();

        if (Request.Headers.IfNoneMatch.ToString().Contains(document.Sha256, StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status304NotModified);

        if (!DocumentDownloadWriter.TryParseRange(
                Request.Headers.Range, document.SizeBytes, out var offset, out var length, out var partial))
        {
            Response.Headers.ContentRange = $"bytes */{document.SizeBytes}";
            return StatusCode(StatusCodes.Status416RangeNotSatisfiable);
        }

        var result = await _documentService.OpenDownloadAsync(
            id,
            requirePublic: true,
            partial ? offset : null,
            partial ? length : null,
            ct);

        if (result is null) return NotFound();

        await DocumentDownloadWriter.WriteAsync(
            Response, result, partial ? offset : null, partial ? length : null, publicCache: true, ct);

        await _documentService.IncrementDownloadCountAsync(id, ct);

        return new EmptyResult();
    }
}
