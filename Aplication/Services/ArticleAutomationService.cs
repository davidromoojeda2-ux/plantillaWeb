using System.Text.Json;
using Aplication.DTOs;
using Aplication.DTOs.Automation;
using Aplication.Exceptions;
using Aplication.Interfaces;
using Domine.Entities;
using Microsoft.Extensions.Logging;

namespace Aplication.Services;

public interface IAutomationService
{
    Task<AutomationRun?> RunOnceAsync(AutomationTrigger trigger, CancellationToken ct = default);
    Task<AutomationStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<List<AutomationRun>> GetRunsAsync(int skip = 0, int take = 20, CancellationToken ct = default);
    Task<AutomationRun?> GetRunAsync(string id, CancellationToken ct = default);
    Task<AutomationSettings> GetSettingsAsync(CancellationToken ct = default);
    Task SaveSettingsAsync(AutomationSettings settings, CancellationToken ct = default);
    Task<List<PendingDecisionDto>> GetPendingDecisionsAsync(CancellationToken ct = default);
    Task ResolveDecisionAsync(string runId, string decisionId, bool continueCreating, DateTime? scheduledAt = null, CancellationToken ct = default);
    Task<int> PublishScheduledAsync(DateTime now, CancellationToken ct = default);
}

/// <summary>
/// Orquestador del pipeline de automatización: descubre temáticas, detecta repeticiones,
/// genera artículos con IA y los publica o deja en borrador reutilizando el flujo existente.
/// </summary>
public sealed class ArticleAutomationService : IAutomationService
{
    private const string DefaultAuthor = "Redacción Automática";

    private readonly ITopicDiscoveryService _discovery;
    private readonly IAiArticleGenerator _ai;
    private readonly IArticleService _articleService;
    private readonly IArticleRepository _articleRepository;
    private readonly ICategoryService _categoryService;
    private readonly IAutomationStateRepository _state;
    private readonly AutomationCoordinator _coordinator;
    private readonly ILogger<ArticleAutomationService> _logger;

    public ArticleAutomationService(
        ITopicDiscoveryService discovery,
        IAiArticleGenerator ai,
        IArticleService articleService,
        IArticleRepository articleRepository,
        ICategoryService categoryService,
        IAutomationStateRepository state,
        AutomationCoordinator coordinator,
        ILogger<ArticleAutomationService> logger)
    {
        _discovery = discovery;
        _ai = ai;
        _articleService = articleService;
        _articleRepository = articleRepository;
        _categoryService = categoryService;
        _state = state;
        _coordinator = coordinator;
        _logger = logger;
    }

    public async Task<AutomationRun?> RunOnceAsync(AutomationTrigger trigger, CancellationToken ct = default)
    {
        // Evita corridas concurrentes (worker programado vs. disparo manual del panel).
        if (!await _coordinator.TryBeginRunAsync(ct)) return null;

        // Token propio de la corrida: se cancela al pulsar "Detener" (o al apagar la app).
        var runToken = _coordinator.RunToken;

        try
        {
            var settings = await GetSettingsAsync(ct);

            // Las ejecuciones manuales se permiten aunque la automatización esté deshabilitada:
            // el botón "Ejecutar ahora" es una acción explícita del usuario.
            if (!settings.Enabled && trigger != AutomationTrigger.Manual)
            {
                _logger.LogInformation("Automatización deshabilitada; se omite la corrida programada.");
                return null;
            }

            if (settings.BaseTopics.Count == 0)
            {
                _logger.LogWarning("No hay temáticas base configuradas; no se generan artículos.");
                return null;
            }

            var run = new AutomationRun { Trigger = trigger, Status = AutomationRunStatus.EnProgreso };
            await _state.InsertRunAsync(run);

            try
            {
                IReadOnlyList<DiscoveredTopic> topics;
                if (!settings.DiscoverWithSerper)
                {
                    // Sin Serper: cada temática base es directamente la keyword principal.
                    topics = settings.BaseTopics
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Select(t => new DiscoveredTopic { Title = t.Trim() })
                        .ToList();
                }
                else
                {
                    var discovery = await _discovery.DiscoverAsync(new TopicDiscoveryRequest
                    {
                        Topics = settings.BaseTopics,
                        Country = settings.Country,
                        Language = settings.Language,
                        Limit = Math.Max(1, settings.MaxArticlesPerRun * 2),
                        ApiKey = settings.SerperApiKey,
                        Num = Math.Max(1, settings.SerperNum),
                        Recency = settings.SerperRecency,
                        SearchType = settings.SerperType,
                        Safe = settings.SerperSafe,
                        Autocorrect = settings.SerperAutocorrect,
                        RoundRobin = settings.RoundRobinTopics
                    }, runToken);
                    run.SerperCredits += discovery.Credits;
                    topics = discovery.Topics;
                }

                run.DiscoveredTopicsCount = topics.Count;

                var generated = 0;
                foreach (var topic in topics)
                {
                    if (generated >= settings.MaxArticlesPerRun) break;
                    runToken.ThrowIfCancellationRequested();

                    var hash = SlugGenerator.Hash(topic.Title);
                    run.TopicHashes.Add(hash);

                    var slug = SlugGenerator.Slugify(topic.Title);
                    var isDuplicate = await _state.HashExistsAsync(hash)
                                      || await _articleRepository.SlugExistsAsync(slug);

                    if (isDuplicate)
                    {
                        run.PendingDecisions.Add(new AutomationDecision
                        {
                            TopicTitle = topic.Title,
                            Source = topic.Source
                        });
                        continue;
                    }

                    try
                    {
                        var (info, pendingDecision, totalTokens) = await GenerateAndCreateAsync(topic, settings, null, runToken);
                        AccumulateUsage(run, totalTokens);
                        if (info is not null)
                        {
                            run.GeneratedArticles.Add(info);
                            generated++;
                        }
                        else if (pendingDecision is not null)
                        {
                            run.PendingDecisions.Add(pendingDecision);
                        }
                    }
                    catch (OperationCanceledException) when (runToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        run.Errors.Add($"Error generando '{topic.Title}': {ex.Message}");
                    }
                }

                run.Status = AutomationRunStatus.Completado;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                run.Status = AutomationRunStatus.Fallido;
                run.Errors.Add("Corrida cancelada por el cierre de la aplicación.");
                throw;
            }
            catch (OperationCanceledException) when (runToken.IsCancellationRequested)
            {
                run.Status = AutomationRunStatus.Detenido;
                run.Errors.Add("Corrida detenida por el usuario.");
            }
            catch (Exception ex)
            {
                run.Status = AutomationRunStatus.Fallido;
                run.Errors.Add(ex.Message);
                _logger.LogError(ex, "La corrida de automatización terminó con un error.");
            }
            finally
            {
                run.FinishedAt = DateTime.UtcNow;
                await _state.UpdateRunAsync(run);
            }

            return run;
        }
        finally
        {
            _coordinator.EndRun();
        }
    }

    public async Task<AutomationStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var settings = await GetSettingsAsync(ct);
        var pending = await _state.GetPendingDecisionsAsync();
        var runs = await _state.GetRunsAsync(0, 1);
        var (totalTokens, totalCredits, runCount) = await _state.GetUsageTotalsAsync();

        return new AutomationStatusDto
        {
            Paused = settings.Paused,
            IsRunning = _coordinator.IsBusy,
            AutoPublish = settings.AutoPublish,
            FrequencyMinutes = settings.FrequencyMinutes,
            PendingDecisionsCount = pending.Count,
            LastRun = runs.FirstOrDefault() is { } r
                ? new AutomationRunSummary
                {
                    Status = r.Status.ToString(),
                    Trigger = r.Trigger.ToString(),
                    GeneratedCount = r.GeneratedArticles.Count,
                    TotalTokens = r.TotalTokens
                }
                : null,
            Usage = new UsageTotalsDto
            {
                TotalTokens = totalTokens,
                TotalCredits = totalCredits,
                RunCount = runCount
            }
        };
    }

    public Task<List<AutomationRun>> GetRunsAsync(int skip = 0, int take = 20, CancellationToken ct = default) =>
        _state.GetRunsAsync(skip, take);

    public Task<AutomationRun?> GetRunAsync(string id, CancellationToken ct = default) =>
        _state.GetRunAsync(id);

    public async Task<AutomationSettings> GetSettingsAsync(CancellationToken ct = default) =>
        await _state.GetSettingsAsync() ?? new AutomationSettings();

    public Task SaveSettingsAsync(AutomationSettings settings, CancellationToken ct = default) =>
        _state.SaveSettingsAsync(settings);

    public async Task<List<PendingDecisionDto>> GetPendingDecisionsAsync(CancellationToken ct = default)
    {
        var items = await _state.GetPendingDecisionsAsync();
        return items.Select(i => new PendingDecisionDto
        {
            RunId = i.Run.Id,
            DecisionId = i.Decision.Id,
            TopicTitle = i.Decision.TopicTitle,
            Source = i.Decision.Source,
            Kind = i.Decision.Kind,
            ValidationErrors = i.Decision.ValidationErrors
        }).ToList();
    }

    public async Task ResolveDecisionAsync(string runId, string decisionId, bool continueCreating, DateTime? scheduledAt = null, CancellationToken ct = default)
    {
        var run = await _state.GetRunAsync(runId)
            ?? throw new NotFoundException("Corrida", runId);

        var decision = run.PendingDecisions.FirstOrDefault(d => d.Id == decisionId)
            ?? throw new NotFoundException("Decisión", decisionId);

        if (continueCreating)
        {
            var settings = await GetSettingsAsync(ct);

            if (decision.Kind == AutomationDecisionKind.Validacion)
            {
                if (string.IsNullOrWhiteSpace(decision.DraftJson))
                    throw new InvalidOperationException("La decisión no contiene el artículo generado.");

                var draft = JsonSerializer.Deserialize<GeneratedArticle>(decision.DraftJson)
                    ?? throw new InvalidOperationException("No se pudo recuperar el artículo generado.");

                var info = await CreateArticleAsync(draft, settings, scheduledAt);
                if (info is null)
                    throw new InvalidOperationException("No se pudo guardar el artículo pendiente (el título no genera un slug válido).");

                run.GeneratedArticles.Add(info);
            }
            else
            {
                var topic = new DiscoveredTopic { Title = decision.TopicTitle, Source = decision.Source };
                var (info, pendingDecision, totalTokens) = await GenerateAndCreateAsync(topic, settings, scheduledAt, ct);
                AccumulateUsage(run, totalTokens);

                if (info is not null)
                    run.GeneratedArticles.Add(info);
                else if (pendingDecision is not null)
                    run.PendingDecisions.Add(pendingDecision);
            }
        }

        decision.Status = continueCreating
            ? AutomationDecisionStatus.Continuado
            : AutomationDecisionStatus.Omitido;

        // El borrador ya no es necesario una vez resuelta la decisión.
        if (decision.Kind == AutomationDecisionKind.Validacion)
            decision.DraftJson = null;

        await _state.UpdateRunAsync(run);
    }

    public Task<int> PublishScheduledAsync(DateTime now, CancellationToken ct = default) =>
        _articleService.PublishScheduledAsync(now);

    private static (string Status, DateTime? ScheduledAt) ResolveStatusAndSchedule(
        AutomationSettings settings, DateTime? explicitScheduledAt)
    {
        if (explicitScheduledAt is not null)
            return ("Programado", explicitScheduledAt.Value.ToUniversalTime());

        if (settings.ScheduleEnabled)
            return ("Programado", NextOccurrence(settings.PublishHour, settings.PublishMinute));

        if (settings.AutoPublish)
            return ("Publicado", null);

        return ("Borrador", null);
    }

    private static DateTime NextOccurrence(int hour, int minute)
    {
        var nowLocal = DateTime.Now;
        var candidate = nowLocal.Date.AddHours(hour).AddMinutes(minute);
        if (candidate <= nowLocal) candidate = candidate.AddDays(1);
        return candidate.ToUniversalTime();
    }

    private static void AccumulateUsage(AutomationRun run, int? totalTokens)
    {
        if (totalTokens is null) return;

        run.TotalTokens += totalTokens.Value;
    }

    private async Task<(GeneratedArticleInfo? Info, AutomationDecision? PendingDecision, int? TotalTokens)> GenerateAndCreateAsync(
        DiscoveredTopic topic, AutomationSettings settings, DateTime? scheduledAt, CancellationToken ct)
    {
        var plan = ArticleGenerationPlanner.FromSettings(settings);
        var internalLinks = await _articleService.GetInternalLinkCandidatesAsync(topic.Title) ?? [];
        var (generated, validationErrors, totalTokens) = await GenerateWithValidationAsync(topic, settings, plan, internalLinks, ct);

        if (validationErrors.Count > 0)
        {
            return (null, new AutomationDecision
            {
                TopicTitle = topic.Title,
                Source = topic.Source,
                Kind = AutomationDecisionKind.Validacion,
                ValidationErrors = validationErrors.ToList(),
                DraftJson = JsonSerializer.Serialize(generated)
            }, totalTokens);
        }

        var info = await CreateArticleAsync(generated, settings, scheduledAt);
        return (info, null, totalTokens);
    }

    /// <summary>
    /// Genera el artículo y lo valida contra el plan efectivo. Si no cumple, reintenta
    /// enviando los errores como indicaciones de corrección hasta el máximo configurado.
    /// Si se agotan los intentos, devuelve el último artículo y sus incumplimientos para
    /// que el usuario decida si desea guardarlo de todos modos.
    /// </summary>
    private async Task<(GeneratedArticle Generated, IReadOnlyList<string> Errors, int TotalTokens)> GenerateWithValidationAsync(
        DiscoveredTopic topic, AutomationSettings settings, ArticleGenerationPlan plan,
        IReadOnlyList<InternalLinkCandidate> internalLinks, CancellationToken ct)
    {
        var totalTokens = 0;
        string? correctionHint = null;

        for (var attempt = 1; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var generated = await _ai.GenerateAsync(new TopicGenerationRequest
            {
                TopicTitle = topic.Title,
                Snippet = topic.Snippet,
                Source = topic.Source,
                Language = settings.Language,
                TargetWords = plan.TargetWords,
                MaxTokens = plan.MaxTokens,
                Template = plan.Template,
                Tone = plan.Tone,
                PrimaryKeyword = topic.Title,
                InternalLinks = [.. internalLinks],
                MinBlocks = plan.MinBlocks,
                IncludeList = plan.IncludeList,
                IncludeTable = plan.IncludeTable,
                IncludeQuote = plan.IncludeQuote,
                IncludeConclusion = plan.IncludeConclusion,
                IncludeFaq = plan.IncludeFaq,
                IncludeCta = plan.IncludeCta,
                IncludeLinks = plan.IncludeLinks,
                Temperature = plan.Temperature,
                CorrectionHint = correctionHint,
                ApiKey = settings.AiApiKey,
                BaseUrl = settings.AiBaseUrl,
                Model = settings.AiModel
            }, ct);

            if (generated.TotalTokens is not null)
                totalTokens += generated.TotalTokens.Value;

            var validation = GeneratedArticleValidator.Validate(generated, plan);
            if (validation.IsValid) return (generated, [], totalTokens);

            correctionHint = validation.ToCorrectionHint();

            if (attempt >= plan.MaxGenerationAttempts)
                return (generated, validation.Errors, totalTokens);

            _logger.LogWarning(
                "El artículo generado para '{Topic}' no cumple la estructura (intento {Attempt}/{Attempts}): {Errors}",
                topic.Title, attempt, plan.MaxGenerationAttempts, string.Join(" | ", validation.Errors));
        }
    }

    private async Task<GeneratedArticleInfo?> CreateArticleAsync(
        GeneratedArticle generated, AutomationSettings settings, DateTime? scheduledAt)
    {
        var slug = SlugGenerator.Slugify(generated.Title);
        if (string.IsNullOrEmpty(slug)) return null;

        var categoryId = await ResolveCategoryAsync(generated.CategorySuggestion, settings);

        var summary = new[] { generated.Summary, generated.MetaDescription, generated.Title }
            .First(s => !string.IsNullOrWhiteSpace(s));

        var (status, scheduledAtFinal) = ResolveStatusAndSchedule(settings, scheduledAt);

        var dto = new ArticleDto
        {
            Title = Truncate(generated.Title.Trim(), 250),
            Slug = slug,
            Summary = Truncate(summary.Trim(), 500),
            Blocks = generated.Blocks,
            Author = DefaultAuthor,
            AuthorRole = DefaultAuthor,
            CategoryId = categoryId,
            Tags = generated.Tags.Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList(),
            Status = status,
            ScheduledAt = scheduledAtFinal
        };

        var created = await _articleService.CreateAsync(dto);
        return new GeneratedArticleInfo
        {
            ArticleId = created.Id,
            Title = created.Title,
            Status = created.Status
        };
    }

    private async Task<string?> ResolveCategoryAsync(string? suggestion, AutomationSettings settings)
    {
        var categories = await _categoryService.GetAllAsync();
        if (categories.Count == 0) return null;

        if (!string.IsNullOrWhiteSpace(suggestion))
        {
            var match = categories.FirstOrDefault(c =>
                string.Equals(c.Name, suggestion, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Slug, suggestion, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match.Id;
        }

        foreach (var id in settings.CategoryIds)
        {
            if (categories.Any(c => c.Id == id)) return id;
        }

        return null;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
