using Aplication.Configuration;
using Aplication.Content;
using Aplication.DTOs;
using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;
using Aplication.Exceptions;
using Aplication.Interfaces;
using Aplication.Mappers;
using Aplication.Serialization;
using Aplication.Services;
using Domine.Exceptions;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using paginaWeb.Areas.Admin.Models;
using paginaWeb.Helpers;
using System.Text.Json;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/articulos")]
public class ArticulosController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ImageService _imageService;
    private readonly IDocumentService _documentService;
    private readonly IMenuService _menuService;
    private readonly IAiArticleRewriter _rewriter;
    private readonly IAiArticleCorrector _corrector;
    private readonly IAutomationService _automation;
    private readonly ISiteSettingsService _siteSettingsService;
    private readonly IArticleLinkChecker _linkChecker;

    public ArticulosController(
        IArticleService articleService,
        ICategoryService categoryService,
        ImageService imageService,
        IDocumentService documentService,
        IMenuService menuService,
        IAiArticleRewriter rewriter,
        IAiArticleCorrector corrector,
        IAutomationService automation,
        ISiteSettingsService siteSettingsService,
        IArticleLinkChecker linkChecker)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _imageService = imageService;
        _documentService = documentService;
        _menuService = menuService;
        _rewriter = rewriter;
        _corrector = corrector;
        _automation = automation;
        _siteSettingsService = siteSettingsService;
        _linkChecker = linkChecker;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var articles = await _articleService.GetAllAdminAsync(page, 50);
        var categories = await _categoryService.GetAllAsync();
        ViewBag.Categories = categories.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.Page = page;

        var coverIds = articles
            .Select(a => !string.IsNullOrEmpty(a.CoverImageId)
                ? a.CoverImageId
                : ContentImageResolver.FindFirstImage(a.Blocks)?.ImageId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct()
            .ToList();
        var images = await _imageService.GetVariantsDictAsync(coverIds);
        ViewBag.CoverImages = ImageMapper.ToThumbnailDictionary(images);

        return View(articles);
    }

    [HttpPost("toggle-destacado/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeatured(string id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article is null) return NotFound();

        var editDto = ArticleMapper.ToEditDto(article);
        bool willBeFeatured = !editDto.Featured;

        if (willBeFeatured)
        {
            var (isValid, errorMsg) = await ValidateHeroEligibilityAsync(
                editDto.CoverImageId, editDto.CoverImageUrl, editDto.Blocks, id);

            if (!isValid && errorMsg != null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = errorMsg });
                }
                TempData["Error"] = errorMsg;
                return RedirectToAction(nameof(Index));
            }
        }

        editDto.Featured = willBeFeatured;

        await _articleService.UpdateAsync(id, editDto);
        _menuService.InvalidateMenu();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
        {
            var allUpdated = await _articleService.GetAllAdminAsync(1, 100);
            int totalFeatured = allUpdated.Count(a => a.Featured);
            int activeFeatured = allUpdated.Count(a => a.Featured && string.Equals(a.Status, "Publicado", StringComparison.OrdinalIgnoreCase));
            return Json(new { success = true, featured = editDto.Featured, totalFeatured, activeFeatured });
        }

        TempData["Success"] = editDto.Featured
            ? "Artículo fijado como destacado en el Hero principal."
            : "Artículo removido de los destacados del Hero.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("nuevo")]
    public async Task<IActionResult> Create()
    {
        var model = new ArticleFormViewModel
        {
            Author = User.Identity?.Name ?? "Redacción",
            Status = "Publicado"
        };
        await PopulateCollectionsAsync(model);
        return View(model);
    }

    [HttpPost("nuevo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticleFormViewModel model)
    {
        ContentBlockFormHelper.TryParse(model.BlocksJson, ModelState, out var blocks);
        await ValidateFeaturedHeroAsync(model, blocks);

        if (!ModelState.IsValid)
        {
            await PopulateCollectionsAsync(model);
            return View(model);
        }

        try
        {
            await _articleService.CreateAsync(model.ToDto(blocks));
            _menuService.InvalidateMenu();
            TempData["Success"] = "Artículo publicado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            await PopulateCollectionsAsync(model);
            return View(model);
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            await PopulateCollectionsAsync(model);
            return View(model);
        }
    }

    [HttpGet("editar/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article is null) return NotFound();

        string? coverUrl = await ResolveCoverImageUrlAsync(article);

        var model = ArticleFormViewModel.FromDto(article, coverUrl);
        await PopulateCollectionsAsync(model, id);

        return View(model);
    }


    [HttpPost("editar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ArticleFormViewModel model)
    {
        model.Id = id;
        ContentBlockFormHelper.TryParse(model.BlocksJson, ModelState, out var blocks);
        await ValidateFeaturedHeroAsync(model, blocks, id);

        if (!ModelState.IsValid)
        {
            await PopulateCollectionsAsync(model, id);
            return View(model);
        }

        try
        {
            await _articleService.UpdateAsync(id, model.ToDto(blocks));
            _menuService.InvalidateMenu();
            TempData["Success"] = "Artículo actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            await PopulateCollectionsAsync(model, id);
            return View(model);
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            await PopulateCollectionsAsync(model, id);
            return View(model);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("validar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validar([FromBody] ArticleValidationRequest request)
    {
        if (request is null)
            return BadRequest(new { message = "Solicitud de validación no válida." });

        var issues = new List<ArticleValidationIssue>();
        var blocks = new List<ContentBlockDto>();

        try
        {
            blocks = ContentBlockJson.Deserialize(request.BlocksJson);
        }
        catch (JsonException ex)
        {
            issues.Add(new ArticleValidationIssue(
                ArticleIssueSeverity.Error, "json", ex.Message, "contentBlocksEditor"));
        }

        var validation = ArticleFormValidator.Validate(
            request.Title,
            request.Summary,
            request.Slug,
            request.Author,
            request.CategoryId,
            request.CoverImageUrl,
            request.Status,
            blocks,
            blocks.Count > 0 ? await ResolveParagraphStyleAsync() : null);

        issues.AddRange(validation.EffectiveIssues);

        var linksToCheck = validation.Links.ToList();
        if (!string.IsNullOrWhiteSpace(request.CoverImageUrl))
            linksToCheck.Add(request.CoverImageUrl);

        var baseUri = new Uri($"{Request.Scheme}://{Request.Host}");
        var links = await _linkChecker.CheckAsync(linksToCheck, baseUri, HttpContext.RequestAborted);

        foreach (var link in links)
        {
            var isCover = !string.IsNullOrWhiteSpace(request.CoverImageUrl)
                && string.Equals(link.Url, request.CoverImageUrl, StringComparison.Ordinal);

            if (link.IsBroken)
            {
                issues.Add(new ArticleValidationIssue(
                    ArticleIssueSeverity.Error,
                    "link",
                    $"Enlace caído: {link.Url} ({link.Detail ?? link.Status}).",
                    isCover ? "coverImageUrlInput" : null,
                    isCover ? null : ArticleFormValidator.FindBlocksWithLink(blocks, link.Url, baseUri),
                    link.Url));
            }
            else if (link.IsWarning)
            {
                issues.Add(new ArticleValidationIssue(
                    ArticleIssueSeverity.Warning,
                    "link",
                    $"Enlace restringido: {link.Url} ({link.StatusCode}).",
                    isCover ? "coverImageUrlInput" : null,
                    isCover ? null : ArticleFormValidator.FindBlocksWithLink(blocks, link.Url, baseUri),
                    link.Url));
            }
        }

        var errors = issues
            .Where(issue => issue.Severity == ArticleIssueSeverity.Error)
            .Select(issue => issue.Message)
            .ToList();
        var warnings = issues
            .Where(issue => issue.Severity == ArticleIssueSeverity.Warning)
            .Select(issue => issue.Message)
            .ToList();

        return Json(new
        {
            isValid = errors.Count == 0,
            errors,
            warnings,
            issues = issues.Select(issue => new
            {
                severity = issue.Severity,
                code = issue.Code,
                message = issue.Message,
                field = issue.Field,
                blockIndexes = issue.BlockIndexes,
                url = issue.Url
            }),
            links = links.Select(link => new
            {
                link.Url,
                link.StatusCode,
                link.Status,
                link.Detail,
                link.IsBroken
            }),
            checkedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
        });
    }

    [HttpPost("corregir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Corregir([FromBody] ArticleCorrectionInput request)
    {
        if (request is null)
            return BadRequest(new { message = "Solicitud de corrección no válida." });

        var settings = await _automation.GetSettingsAsync();
        if (string.IsNullOrWhiteSpace(settings.AiApiKey))
        {
            return BadRequest(new
            {
                message = "La IA no está configurada. Añade una clave en la configuración de automatización."
            });
        }

        List<ContentBlockDto> blocks;
        try
        {
            blocks = ContentBlockJson.Deserialize(request.BlocksJson);
        }
        catch (JsonException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var plan = ArticleGenerationPlanner.FromSettings(settings);

        var issues = (request.Issues ?? [])
            // El panel agrupa el estilo en mensajes sin bloque; el pipeline los
            // regenera con el índice y el detalle exactos.
            .Where(issue => !(issue.Code?.StartsWith("style-", StringComparison.Ordinal) ?? false))
            // Los enlaces se gestionan aparte y solo se quitan con confirmación:
            // el corrector no debe tocarlos ni recibirlos como instrucción.
            .Where(issue => !string.Equals(issue.Code, "link", StringComparison.OrdinalIgnoreCase))
            .Select(issue => issue.Message?.Trim())
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message!)
            .ToList();

        try
        {
            var outcome = await ArticleCorrectionPipeline.CorrectAsync(
                new GeneratedArticle
                {
                    Title = request.Title ?? string.Empty,
                    Summary = request.Summary ?? string.Empty,
                    Blocks = blocks
                },
                issues,
                plan,
                new ArticleCorrectionRequest
                {
                    Temperature = plan.Temperature,
                    MaxTokens = plan.MaxTokens,
                    ApiKey = settings.AiApiKey,
                    BaseUrl = settings.AiBaseUrl,
                    Model = settings.AiModel
                },
                _corrector,
                HttpContext.RequestAborted);

            var corrected = outcome.Article;

            // Los enlaces caídos no se eliminan automáticamente: se devuelven para
            // que el usuario confirme antes de quitar las etiquetas.
            var baseUri = new Uri($"{Request.Scheme}://{Request.Host}");
            var correctedLinks = ArticleFormValidator.ExtractLinks(corrected.Blocks);
            var brokenLinks = new List<string>();
            if (correctedLinks.Count > 0)
            {
                var checkedLinks = await _linkChecker.CheckAsync(
                    correctedLinks, baseUri, HttpContext.RequestAborted);

                brokenLinks = checkedLinks
                    .Where(link => link.Status == "caido")
                    .Select(link => link.Url)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
            }

            return Json(new
            {
                title = string.IsNullOrWhiteSpace(corrected.Title)
                    ? request.Title ?? string.Empty
                    : corrected.Title,
                summary = string.IsNullOrWhiteSpace(corrected.Summary)
                    ? request.Summary ?? string.Empty
                    : corrected.Summary,
                metaDescription = corrected.MetaDescription,
                tags = corrected.Tags,
                blocksJson = ContentBlockJson.Serialize(corrected.Blocks),
                brokenLinks,
                remainingStyleIssues = outcome.RemainingFindings.Count,
                attempts = outcome.Attempts
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"No se pudo corregir el artículo: {ex.Message}" });
        }
    }

    [HttpPost("quitar-enlaces")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarEnlaces([FromBody] ArticleLinkRemovalInput request)
    {
        if (request is null)
            return BadRequest(new { message = "Solicitud no válida." });

        if (!request.Confirmed)
        {
            return BadRequest(new
            {
                message = "La eliminación de enlaces requiere confirmación explícita del usuario."
            });
        }

        List<ContentBlockDto> blocks;
        try
        {
            blocks = ContentBlockJson.Deserialize(request.BlocksJson);
        }
        catch (JsonException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var urls = (request.Urls ?? [])
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (urls.Count == 0)
            return Json(new { blocksJson = ContentBlockJson.Serialize(blocks), removed = 0 });

        var baseUri = new Uri($"{Request.Scheme}://{Request.Host}");
        var checkedLinks = await _linkChecker.CheckAsync(
            urls, baseUri, HttpContext.RequestAborted);

        // Solo se quitan los 404/410 confirmados en este momento.
        var broken = checkedLinks
            .Where(link => link.Status == "caido")
            .Select(link => link.Url)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (broken.Count > 0)
            ArticleFormValidator.RemoveLinks(blocks, broken, baseUri);

        return Json(new
        {
            blocksJson = ContentBlockJson.Serialize(blocks),
            removed = broken.Count
        });
    }

    [HttpPost("eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _articleService.DeleteAsync(id);
            _menuService.InvalidateMenu();
            TempData["Success"] = "Artículo eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("reescribir/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reescribir(string id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article is null) return NotFound();

        try
        {
            var settings = await _automation.GetSettingsAsync();
            var plan = ArticleGenerationPlanner.FromSettings(settings);
            var internalLinks = await _articleService.GetInternalLinkCandidatesAsync(article.Title) ?? [];

            var rewritten = await _rewriter.RewriteAsync(new ArticleRewriteRequest
            {
                Title = article.Title,
                Summary = article.Summary,
                Blocks = article.Blocks,
                Language = settings.Language,
                Tone = plan.Tone,
                Template = plan.Template,
                TargetWords = plan.TargetWords,
                MinBlocks = plan.MinBlocks,
                MaxTokens = plan.MaxTokens,
                IncludeList = plan.IncludeList,
                IncludeTable = plan.IncludeTable,
                IncludeQuote = plan.IncludeQuote,
                IncludeConclusion = plan.IncludeConclusion,
                IncludeFaq = plan.IncludeFaq,
                IncludeCta = plan.IncludeCta,
                IncludeLinks = plan.IncludeLinks,
                ParagraphStyles = plan.ParagraphStyles,
                InternalLinks = internalLinks,
                Temperature = plan.Temperature,
                ApiKey = settings.AiApiKey,
                BaseUrl = settings.AiBaseUrl,
                Model = settings.AiModel
            });

            var draftStyle = ArticleDraftPipeline.Process(rewritten, plan);

            var slugBase = SlugGenerator.Slugify(rewritten.Title);
            var slug = string.IsNullOrWhiteSpace(slugBase)
                ? $"reescritura-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : $"{slugBase}-reescrito-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var summary = new[] { rewritten.Summary, rewritten.MetaDescription, rewritten.Title }
                .First(s => !string.IsNullOrWhiteSpace(s));

            await _articleService.CreateAsync(new ArticleDto
            {
                Title = rewritten.Title.Trim(),
                Slug = slug,
                Summary = summary.Trim(),
                Blocks = rewritten.Blocks,
                Author = article.Author,
                AuthorBio = article.AuthorBio,
                AuthorAvatarUrl = article.AuthorAvatarUrl,
                AuthorRole = article.AuthorRole,
                AuthorSocialUrl = article.AuthorSocialUrl,
                CategoryId = article.CategoryId,
                SubcategoryId = article.SubcategoryId,
                Tags = rewritten.Tags,
                CoverImageId = article.CoverImageId,
                CoverImageUrl = article.CoverImageUrl,
                ImageIds = [.. article.ImageIds],
                Featured = false,
                Status = "Borrador"
            });

            _menuService.InvalidateMenu();

            // El estilo de párrafo nunca impide guardar: se informa como aviso en el toast.
            var styleNotes = ParagraphStyle.Summarize(draftStyle.Findings, draftStyle.Profile);
            TempData["Success"] = "Artículo reescrito y guardado como borrador. Revísalo antes de publicar."
                + (styleNotes.Count > 0
                    ? " " + string.Join(" ", styleNotes)
                    : string.Empty);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"No se pudo reescribir el artículo: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("upload-inline")]
    public async Task<IActionResult> UploadInline(IFormFile file, string? articleId = null)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No se ha proporcionado ningún archivo." });

        using var stream = file.OpenReadStream();
        var metadata = await _imageService.ProcessAndStoreDtoAsync(
            file.FileName, stream, file.FileName, articleId);

        var url = ImageMapper.GetBestUrl(metadata) ?? metadata.Variants.FirstOrDefault()?.PublicUrl;

        return Json(new { id = metadata.Id, url = url, alt = metadata.Alt });
    }

    [HttpGet("imagenes-disponibles")]
    public async Task<IActionResult> GetAvailableImages(string? currentArticleId = null)
    {
        var result = await ImageCatalogHelper.BuildAsync(
            _imageService, _articleService, currentArticleId, imageTake: 100, articleTake: 200);

        return Json(result);
    }

    private async Task<List<ImageMetadataResponseDto>> GetImagesWithArticleUsageAsync()
    {
        var images = await _imageService.GetAllAsync(0, 100);
        var allArticles = await _articleService.GetAllAdminAsync(1, 200);

        ImageCatalogHelper.ApplyArticleUsage(images, allArticles);

        return images;
    }

    private async Task PopulateCollectionsAsync(ArticleFormViewModel model, string? id = null)
    {
        var allCategories = await _categoryService.GetAllAsync();
        model.ParentCategories = allCategories.Where(c => c.ParentId is null).OrderBy(c => c.Order).ToList();
        model.SubCategories = allCategories.Where(c => c.ParentId is not null).OrderBy(c => c.Order).ToList();
        model.LinkedImages = !string.IsNullOrEmpty(id) ? await _imageService.GetByArticleIdAsync(id) : [];
        model.AvailableImages = await GetImagesWithArticleUsageAsync();

        var allDocuments = await _documentService.GetAllAsync(0, 500);
        var documentsById = allDocuments.ToDictionary(document => document.Id);
        model.AvailableDocuments = allDocuments;
        model.LinkedDocuments = model.DocumentIds
            .Select(documentId => documentsById.TryGetValue(documentId, out var document) ? document : null)
            .Where(document => document is not null)
            .Select(document => document!)
            .ToList();

        var siteSettings = await _siteSettingsService.GetAsync();
        ViewBag.EnabledShareNetworks = siteSettings.EnabledShareNetworks;
    }

    /// <summary>
    /// Resuelve el perfil de párrafo vigente con la plantilla y los umbrales guardados.
    /// El editor no envía la plantilla, así que se usa la configuración global.
    /// </summary>
    private async Task<ParagraphStyleProfile> ResolveParagraphStyleAsync()
    {
        var settings = await _automation.GetSettingsAsync();
        var plan = ArticleGenerationPlanner.FromSettings(settings);
        return ParagraphStyleProfile.Resolve(plan.Template, plan.ParagraphStyles, settings.Language);
    }

    private async Task<(bool IsValid, string? ErrorMessage)> ValidateHeroEligibilityAsync(
        string? coverImageId,
        string? coverImageUrl,
        IEnumerable<ContentBlockDto>? blocks,
        string? excludeArticleId = null)
    {
        bool hasImage = !string.IsNullOrEmpty(coverImageId)
                     || !string.IsNullOrWhiteSpace(coverImageUrl)
                     || ContentImageResolver.HasImage(blocks);

        if (!hasImage)
        {
            return (false, "No se puede designar en el Hero: El artículo debe contar con una imagen asignada.");
        }

        var allAdmin = await _articleService.GetAllAdminAsync(1, 100);
        int currentFeatured = allAdmin.Count(a => a.Featured && (excludeArticleId == null || a.Id != excludeArticleId));
        if (currentFeatured >= ArticleDisplayLimits.HeroFeaturedLimit)
        {
            return (false, $"Límite alcanzado: El Hero principal solo permite un máximo de {ArticleDisplayLimits.HeroFeaturedLimit} artículos destacados. Debes desmarcar uno antes de designar otro.");
        }

        return (true, null);
    }

    private async Task ValidateFeaturedHeroAsync(
        ArticleFormViewModel model,
        IEnumerable<ContentBlockDto>? blocks,
        string? excludeArticleId = null)
    {
        if (!model.Featured) return;

        var (isValid, errorMessage) = await ValidateHeroEligibilityAsync(
            model.CoverImageId, model.CoverImageUrl, blocks, excludeArticleId);

        if (!isValid && errorMessage != null)
        {
            ModelState.AddModelError("Featured", errorMessage);
        }
    }

    private async Task<string?> ResolveCoverImageUrlAsync(ArticleResponseDto article)
    {
        if (!string.IsNullOrEmpty(article.CoverImageUrl)) return article.CoverImageUrl;

        if (!string.IsNullOrEmpty(article.CoverImageId))
        {
            var coverImg = await _imageService.GetByIdAsync(article.CoverImageId);
            var bestUrl = ImageMapper.GetBestUrl(coverImg);
            if (!string.IsNullOrEmpty(bestUrl)) return bestUrl;
        }

        if (article.ImageIds.Count > 0)
        {
            var firstImg = await _imageService.GetByIdAsync(article.ImageIds.First());
            if (firstImg != null)
            {
                var bestUrl = ImageMapper.GetBestUrl(firstImg);
                if (!string.IsNullOrEmpty(bestUrl)) return bestUrl;
            }
        }

        var firstBodyImage = ContentImageResolver.FindFirstImage(article.Blocks);
        if (firstBodyImage is not null)
        {
            if (!string.IsNullOrWhiteSpace(firstBodyImage.ImageId))
            {
                var bodyImg = await _imageService.GetByIdAsync(firstBodyImage.ImageId);
                var bodyUrl = ImageMapper.GetBestUrl(bodyImg);
                if (!string.IsNullOrEmpty(bodyUrl)) return bodyUrl;
            }

            if (!string.IsNullOrWhiteSpace(firstBodyImage.Url))
                return firstBodyImage.Url;
        }

        return null;
    }
}

