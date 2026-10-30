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

        try
        {
            var settings = await GetSettingsAsync(ct);
            if (!settings.Enabled)
            {
                _logger.LogInformation("Automatización deshabilitada; se omite la corrida.");
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
                var topics = await _discovery.DiscoverAsync(new TopicDiscoveryRequest
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
                    Autocorrect = settings.SerperAutocorrect
                }, ct);
                run.DiscoveredTopicsCount = topics.Count;

                var generated = 0;
                foreach (var topic in topics)
                {
                    if (generated >= settings.MaxArticlesPerRun) break;
                    ct.ThrowIfCancellationRequested();

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
                            Source = topic.Source,
                            TopicHash = hash
                        });
                        continue;
                    }

                    try
                    {
                        var info = await GenerateAndCreateAsync(topic, settings, null, ct);
                        if (info is not null)
                        {
                            run.GeneratedArticles.Add(info);
                            generated++;
                        }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
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
                run.Errors.Add("Corrida cancelada.");
                throw;
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

        return new AutomationStatusDto
        {
            Enabled = settings.Enabled,
            Paused = settings.Paused,
            IsRunning = _coordinator.IsBusy,
            AutoPublish = settings.AutoPublish,
            FrequencyMinutes = settings.FrequencyMinutes,
            PendingDecisionsCount = pending.Count,
            LastRun = runs.FirstOrDefault() is { } r
                ? new AutomationRunSummary
                {
                    Id = r.Id,
                    Status = r.Status.ToString(),
                    Trigger = r.Trigger.ToString(),
                    StartedAt = r.StartedAt,
                    DiscoveredTopicsCount = r.DiscoveredTopicsCount,
                    GeneratedCount = r.GeneratedArticles.Count
                }
                : null
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
            CreatedAt = i.Decision.CreatedAt
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
            var topic = new DiscoveredTopic { Title = decision.TopicTitle, Source = decision.Source };
            var info = await GenerateAndCreateAsync(topic, settings, scheduledAt, ct);
            if (info is not null)
            {
                run.GeneratedArticles.Add(info);
                await _state.UpdateRunAsync(run);
            }
        }

        await _state.ResolveDecisionAsync(
            runId, decisionId,
            continueCreating ? AutomationDecisionStatus.Continuado : AutomationDecisionStatus.Omitido);
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

    private async Task<GeneratedArticleInfo?> GenerateAndCreateAsync(
        DiscoveredTopic topic, AutomationSettings settings, DateTime? scheduledAt, CancellationToken ct)
    {
        var generated = await _ai.GenerateAsync(new TopicGenerationRequest
        {
            TopicTitle = topic.Title,
            Snippet = topic.Snippet,
            Source = topic.Source,
            Language = settings.Language,
            MaxTokens = settings.MaxTokensPerArticle,
            ApiKey = settings.AiApiKey,
            BaseUrl = settings.AiBaseUrl,
            Model = settings.AiModel
        }, ct);

        if (string.IsNullOrWhiteSpace(generated.Title) || generated.Blocks.Count == 0)
            return null;

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
