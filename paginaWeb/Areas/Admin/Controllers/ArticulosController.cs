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
        var internalLinks = await _articleService
            .GetInternalLinkCandidatesAsync(request.Title ?? string.Empty) ?? [];

        var issues = (request.Issues ?? [])
            // El panel agrupa el estilo en mensajes sin bloque; el pipeline los
            // regenera con el índice y el detalle exactos.
            .Where(issue => !(issue.Code?.StartsWith("style-", StringComparison.Ordinal) ?? false))
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
                    InternalLinks = internalLinks,
                    Temperature = plan.Temperature,
                    MaxTokens = plan.MaxTokens,
                    ApiKey = settings.AiApiKey,
                    BaseUrl = settings.AiBaseUrl,
                    Model = settings.AiModel
                },
                _corrector,
                HttpContext.RequestAborted);

            var corrected = outcome.Article;

            // La IA propone cambios, pero no se aplican aquí: se validan y se
            // devuelven para que el usuario los confirme. Antes de mostrarla, se
            // restauran los href originales por si el modelo los cambió por su cuenta.
            var baseUri = new Uri($"{Request.Scheme}://{Request.Host}");
            ArticleFormValidator.RestoreLinkHrefs(corrected.Blocks, blocks, baseUri);
            var correctedLinks = ArticleFormValidator.ExtractLinks(corrected.Blocks);

            var urlsToCheck = corrected.LinkActions
                .SelectMany(action => new[] { action.Url, action.Replacement })
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url!)
                .Concat(correctedLinks)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var checkedLinks = urlsToCheck.Count > 0
                ? await _linkChecker.CheckAsync(urlsToCheck, baseUri, HttpContext.RequestAborted)
                : [];

            var checksByCanonical = checkedLinks
                .Select(link => (
                    Canonical: ArticleFormValidator.CanonicalizeLink(link.Url, baseUri),
                    Link: link))
                .Where(item => item.Canonical is not null)
                .GroupBy(item => item.Canonical!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Link,
                    StringComparer.OrdinalIgnoreCase);

            ArticleLinkCheckResult? FindCheck(string? url)
            {
                var canonical = ArticleFormValidator.CanonicalizeLink(url, baseUri);
                return canonical is not null && checksByCanonical.TryGetValue(canonical, out var check)
                    ? check
                    : null;
            }

            bool ExistsInBody(string? url)
            {
                var canonical = ArticleFormValidator.CanonicalizeLink(url, baseUri);
                return canonical is not null && correctedLinks.Any(link =>
                    string.Equals(
                        ArticleFormValidator.CanonicalizeLink(link, baseUri),
                        canonical,
                        StringComparison.OrdinalIgnoreCase));
            }

            var proposed = new List<(
                string Type, string Url, string? Replacement, string? Reason,
                bool Verified, string Validation)>();

            foreach (var action in corrected.LinkActions)
            {
                var type = action.Type.Trim().ToLowerInvariant();

                if (type == "replace")
                {
                    var replacement = action.Replacement?.Trim();
                    if (string.IsNullOrWhiteSpace(replacement))
                    {
                        proposed.Add(("replace", action.Url, null, action.Reason, false,
                            "Falta la URL de reemplazo."));
                        continue;
                    }

                    var check = FindCheck(replacement);
                    var exists = ExistsInBody(action.Url);
                    var verified = exists && check is not null && !check.IsBroken;
                    var validation = !exists
                        ? "El enlace original ya no está en el artículo."
                        : check is null
                            ? "No se pudo comprobar el reemplazo."
                            : check.IsBroken
                                ? "El reemplazo no responde."
                                : $"Reemplazo verificado ({check.StatusCode?.ToString() ?? check.Status}).";

                    proposed.Add(("replace", action.Url, replacement, action.Reason, verified, validation));
                }
                else if (type == "remove")
                {
                    var check = FindCheck(action.Url);
                    var exists = ExistsInBody(action.Url);
                    var verified = exists && check?.Status == "caido";
                    var validation = !exists
                        ? "El enlace original ya no está en el artículo."
                        : verified
                            ? "Enlace caído confirmado."
                            : "El enlace no está caído; no se quitará.";

                    proposed.Add(("remove", action.Url, null, action.Reason, verified, validation));
                }
            }

            // Ningún enlace caído se queda sin propuesta: si la IA no lo cubrió,
            // se ofrece quitarlo, pero el usuario decide.
            foreach (var link in correctedLinks)
            {
                if (FindCheck(link)?.Status != "caido") continue;

                var canonical = ArticleFormValidator.CanonicalizeLink(link, baseUri);
                var covered = proposed.Any(action =>
                    action.Verified
                    && string.Equals(
                        ArticleFormValidator.CanonicalizeLink(action.Url, baseUri),
                        canonical,
                        StringComparison.OrdinalIgnoreCase));

                if (covered) continue;

                proposed.Add(("remove", link, null,
                    "Enlace caído detectado en la validación.", true,
                    "Enlace caído confirmado."));
            }

            var preferred = new List<(
                string Type, string Url, string? Replacement, string? Reason,
                bool Verified, string Validation)>();

            foreach (var group in proposed.GroupBy(action =>
                ArticleFormValidator.CanonicalizeLink(action.Url, baseUri)))
            {
                var replace = group.FirstOrDefault(action =>
                    action.Type == "replace" && action.Verified);

                if (replace.Url is not null)
                {
                    preferred.Add(replace);
                    continue;
                }

                preferred.AddRange(group);
            }

            var linkActions = preferred
                .GroupBy(action => (
                    action.Type,
                    Url: ArticleFormValidator.CanonicalizeLink(action.Url, baseUri),
                    Replacement: ArticleFormValidator.CanonicalizeLink(action.Replacement, baseUri)))
                .Select(group => group.OrderByDescending(action => action.Verified).First())
                .Take(10)
                .ToList();

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
                linkActions = linkActions.Select(action => new
                {
                    type = action.Type,
                    url = action.Url,
                    replacement = action.Replacement,
                    reason = action.Reason,
                    verified = action.Verified,
                    validation = action.Validation
                }),
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

    [HttpPost("aplicar-cambios-enlaces")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AplicarCambiosEnlaces(
        [FromBody] ArticleLinkActionsApplyInput request)
    {
        if (request is null)
            return BadRequest(new { message = "Solicitud no válida." });

        if (!request.Confirmed)
        {
            return BadRequest(new
            {
                message = "Los cambios de enlaces requieren confirmación explícita del usuario."
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

        var actions = (request.Actions ?? [])
            .Where(action => !string.IsNullOrWhiteSpace(action.Url))
            .Select(action => new ArticleLinkActionInput(
                (action.Type ?? string.Empty).Trim().ToLowerInvariant(),
                action.Url!.Trim(),
                string.IsNullOrWhiteSpace(action.Replacement) ? null : action.Replacement.Trim()))
            .ToList();

        if (actions.Count == 0)
        {
            return Json(new
            {
                blocksJson = ContentBlockJson.Serialize(blocks),
                applied = 0,
                skipped = 0
            });
        }

        var baseUri = new Uri($"{Request.Scheme}://{Request.Host}");
        var urls = actions
            .SelectMany(action => new[] { action.Url, action.Replacement })
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var checkedLinks = await _linkChecker.CheckAsync(
            urls, baseUri, HttpContext.RequestAborted);

        var checksByCanonical = checkedLinks
            .Select(link => (
                Canonical: ArticleFormValidator.CanonicalizeLink(link.Url, baseUri),
                Link: link))
            .Where(item => item.Canonical is not null)
            .GroupBy(item => item.Canonical!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Link,
                StringComparer.OrdinalIgnoreCase);

        ArticleLinkCheckResult? FindCheck(string? url)
        {
            var canonical = ArticleFormValidator.CanonicalizeLink(url, baseUri);
            return canonical is not null && checksByCanonical.TryGetValue(canonical, out var check)
                ? check
                : null;
        }

        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var removals = new List<string>();
        var applied = 0;
        var skipped = 0;

        foreach (var action in actions)
        {
            if (action.Type == "replace")
            {
                var check = FindCheck(action.Replacement);
                if (!string.IsNullOrWhiteSpace(action.Replacement)
                    && check is not null
                    && !check.IsBroken)
                {
                    replacements[action.Url] = action.Replacement!;
                    applied++;
                }
                else
                {
                    skipped++;
                }
            }
            else if (action.Type == "remove")
            {
                if (FindCheck(action.Url)?.Status == "caido")
                {
                    removals.Add(action.Url);
                    applied++;
                }
                else
                {
                    skipped++;
                }
            }
            else
            {
                skipped++;
            }
        }

        if (replacements.Count > 0)
            ArticleFormValidator.ReplaceLinks(blocks, replacements, baseUri);

        if (removals.Count > 0)
            ArticleFormValidator.RemoveLinks(blocks, removals, baseUri);

        return Json(new
        {
            blocksJson = ContentBlockJson.Serialize(blocks),
            applied,
            skipped
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

