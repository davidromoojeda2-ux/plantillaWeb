using Aplication.Configuration;
using Aplication.DTOs;
using Aplication.Exceptions;
using Aplication.Interfaces;
using Aplication.Mappers;
using Aplication.Services;
using Domine.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using paginaWeb.Areas.Admin.Models;
using paginaWeb.Helpers;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/documentos")]
public class DocumentosController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly IArticleService _articleService;
    private readonly DocumentsOptions _options;

    public DocumentosController(
        IDocumentService documentService,
        IArticleService articleService,
        IOptions<DocumentsOptions> options)
    {
        _documentService = documentService;
        _articleService = articleService;
        _options = options.Value;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        int page = 1,
        string? search = null,
        string? storage = null,
        string? extension = null,
        string? articleId = null)
    {
        const int pageSize = 20;
        var (documents, total) = await _documentService.GetPagedAsync(
            page, pageSize, search, storage, extension, articleId);

        var articles = await _articleService.GetAllAdminAsync(1, 200);
        DocumentCatalogHelper.ApplyArticleUsage(documents, articles);

        var model = new DocumentLibraryViewModel
        {
            Documents = documents,
            Articles = articles,
            AllowedExtensions = _options.NormalizedExtensions().OrderBy(item => item).ToList(),
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = total,
            Search = search,
            StorageFilter = storage,
            ExtensionFilter = extension,
            ArticleFilter = articleId
        };

        return View(model);
    }

    [HttpGet("listado")]
    public async Task<IActionResult> List(string? currentArticleId = null)
    {
        var result = await DocumentCatalogHelper.BuildAsync(
            _documentService, _articleService, currentArticleId, documentTake: 500, articleTake: 500);

        return Json(result);
    }

    [HttpPost("subir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        IFormFile file,
        string? displayName,
        string? description,
        bool isPublic = false)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Selecciona un documento válido.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var document = await UploadCoreAsync(file, displayName, description, isPublic);
            TempData["Success"] = $"Documento '{document.DisplayName}' guardado correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ResolveError(ex);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("subir-ajax")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAjax(
        IFormFile file,
        string? displayName,
        string? description,
        bool isPublic = false)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { success = false, message = "Selecciona un documento válido." });

        try
        {
            var document = await UploadCoreAsync(file, displayName, description, isPublic);
            return Json(new
            {
                success = true,
                document = new
                {
                    id = document.Id,
                    displayName = document.DisplayName,
                    originalFileName = document.OriginalFileName,
                    description = document.Description,
                    extension = document.Extension,
                    contentType = document.ContentType,
                    sizeBytes = document.SizeBytes,
                    sizeLabel = DocumentMapper.FormatSize(document.SizeBytes),
                    storage = document.Storage,
                    isPublic = document.IsPublic,
                    downloadUrl = $"/admin/documentos/descargar/{document.Id}"
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ResolveError(ex) });
        }
    }

    [HttpPost("renombrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(string id, string displayName, string? description, bool isPublic = false)
    {
        try
        {
            var document = await _documentService.UpdateMetadataAsync(id, displayName, description, isPublic);
            return Json(new
            {
                success = true,
                document = new
                {
                    id = document.Id,
                    displayName = document.DisplayName,
                    description = document.Description,
                    isPublic = document.IsPublic
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ResolveError(ex) });
        }
    }

    [HttpPost("eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _documentService.DeleteAsync(id);
            TempData["Success"] = "El documento y su archivo asociado han sido eliminados.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ResolveError(ex);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar-ajax/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAjax(string id)
    {
        try
        {
            await _documentService.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ResolveError(ex) });
        }
    }

    [HttpGet("descargar/{id}")]
    public async Task<IActionResult> Download(string id, CancellationToken ct)
    {
        var document = await _documentService.GetByIdAsync(id, ct);
        if (document is null) return NotFound();

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
            requirePublic: false,
            partial ? offset : null,
            partial ? length : null,
            ct);

        if (result is null) return NotFound();

        await DocumentDownloadWriter.WriteAsync(
            Response, result, partial ? offset : null, partial ? length : null, publicCache: false, ct);

        return new EmptyResult();
    }

    private async Task<DocumentResponseDto> UploadCoreAsync(
        IFormFile file,
        string? displayName,
        string? description,
        bool isPublic)
    {
        await using var stream = file.OpenReadStream();
        return await _documentService.UploadAsync(new DocumentUploadRequest
        {
            Content = stream,
            OriginalFileName = file.FileName,
            DisplayName = displayName,
            Description = description,
            IsPublic = isPublic
        });
    }

    private static string ResolveError(Exception ex) => ex switch
    {
        DomainValidationException => ex.Message,
        ConflictException => ex.Message,
        NotFoundException => ex.Message,
        InvalidOperationException => ex.Message,
        _ => "No se pudo procesar el documento: " + ex.Message
    };
}
