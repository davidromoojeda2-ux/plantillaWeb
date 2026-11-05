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
    private readonly ITopicCountryValidator _countryValidator;
    private readonly IArticleService _articleService;
    private readonly IArticleRepository _articleRepository;
    private readonly ICategoryService _categoryService;
    private readonly IAutomationStateRepository _state;
    private readonly AutomationCoordinator _coordinator;
    private readonly ILogger<ArticleAutomationService> _logger;

    public ArticleAutomationService(
        ITopicDiscoveryService discovery,
        IAiArticleGenerator ai,
        ITopicCountryValidator countryValidator,
        IArticleService articleService,
        IArticleRepository articleRepository,
        ICategoryService categoryService,
        IAutomationStateRepository state,
        AutomationCoordinator coordinator,
        ILogger<ArticleAutomationService> logger)
    {
        _discovery = discovery;
        _ai = ai;
        _countryValidator = countryValidator;
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
                var baseTopics = settings.BaseTopics
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Con round-robin se garantiza al menos un artículo por temática base,
                // aunque el máximo configurado sea menor que el número de temáticas.
                var targetCount = settings.RoundRobinTopics
                    ? Math.Max(settings.MaxArticlesPerRun, baseTopics.Count)
                    : settings.MaxArticlesPerRun;

                IReadOnlyList<DiscoveredTopic> topics;
                if (!settings.DiscoverWithSerper)
                {
                    // Sin Serper: cada temática base es directamente la keyword principal.
                    topics = baseTopics
                        .Select(t => new DiscoveredTopic { Title = t, BaseTopic = t })
                        .ToList();
                }
                else
                {
                    var discovery = await _discovery.DiscoverAsync(new TopicDiscoveryRequest
                    {
                        Topics = baseTopics,
                        Country = settings.Country,
                        Language = settings.Language,
                        Limit = Math.Max(1, targetCount * 2),
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
                var groups = BuildTopicGroups(topics, baseTopics, settings.RoundRobinTopics);

                if (settings.RoundRobinTopics)
                {
                    foreach (var baseTopic in baseTopics)
                    {
                        if (!groups.Any(g => string.Equals(g.Key, baseTopic, StringComparison.OrdinalIgnoreCase)))
                            _logger.LogWarning("Serper no devolvió candidatos para la temática base '{Topic}'.", baseTopic);
                    }
                }

                // Fase 1: una oportunidad real por temática. Si el primer candidato ya existe,
                // se busca el siguiente de la misma temática antes de pasar a la siguiente.
                // Una temática descartada por país no vuelve a intentarse en esta corrida.
                var skippedBaseTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (settings.RoundRobinTopics)
                {
                    foreach (var group in groups)
                    {
                        if (generated >= targetCount) break;

                        while (group.HasNext)
                        {
                            var outcome = await ProcessNextCandidateAsync(
                                run, group, settings, skippedBaseTopics, runToken);
                            if (outcome == TopicOutcome.Generated) generated++;
                            if (outcome != TopicOutcome.Duplicate) break;
                        }
                    }
                }

                // Fase 2: completa el cupo restante repartiendo los candidatos entre temáticas.
                while (generated < targetCount)
                {
                    var progressed = false;

                    foreach (var group in groups)
                    {
                        if (generated >= targetCount) break;
                        if (group.Skipped || !group.HasNext) continue;

                        progressed = true;
                        var outcome = await ProcessNextCandidateAsync(
                            run, group, settings, skippedBaseTopics, runToken);
                        if (outcome == TopicOutcome.Generated) generated++;
                    }

                    if (!progressed) break;
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

    /// <summary>
    /// Procesa un candidato: omite duplicados y genera/registra el artículo.
    /// Devuelve el resultado para que la corrida decida si debe probar otro candidato.
    /// </summary>
    private async Task<TopicOutcome> ProcessTopicAsync(
        AutomationRun run, DiscoveredTopic topic, AutomationSettings settings, CancellationToken ct)
    {
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
                Source = topic.Source
            });
            return TopicOutcome.Duplicate;
        }

        // El filtro de país de Serper (gl) es solo una pista de localización: antes de
        // redactar se confirma con IA que el tema corresponde al país configurado.
        if (settings.DiscoverWithSerper)
        {
            var check = await ValidateCountryAsync(run, topic, settings, ct);
            if (!check) return TopicOutcome.NotRelevant;
        }

        try
        {
            var (info, pendingDecision, totalTokens) = await GenerateAndCreateAsync(topic, settings, null, ct);
            AccumulateUsage(run, totalTokens);

            if (info is not null)
            {
                run.GeneratedArticles.Add(info);
                return TopicOutcome.Generated;
            }

            if (pendingDecision is not null)
            {
                run.PendingDecisions.Add(pendingDecision);
                return TopicOutcome.Pending;
            }

            return TopicOutcome.Failed;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            run.Errors.Add($"Error generando '{topic.Title}': {ex.Message}");
            return TopicOutcome.Failed;
        }
    }

    /// <summary>
    /// Toma el siguiente candidato del grupo y aplica la exclusión por país: si la temática
    /// ya fue descartada se evita volver a procesarla, y si este candidato resulta de otro
    /// país se marca el grupo como omitido (round-robin) para no intentar más candidatos.
    /// </summary>
    private async Task<TopicOutcome> ProcessNextCandidateAsync(
        AutomationRun run, TopicGroup group, AutomationSettings settings,
        HashSet<string> skippedBaseTopics, CancellationToken ct)
    {
        var topic = group.Next()!;
        var key = BaseTopicKey(topic);

        if (skippedBaseTopics.Contains(key))
        {
            if (settings.RoundRobinTopics) group.Skipped = true;
            return TopicOutcome.NotRelevant;
        }

        var outcome = await ProcessTopicAsync(run, topic, settings, ct);
        if (outcome == TopicOutcome.NotRelevant)
        {
            skippedBaseTopics.Add(key);
            if (settings.RoundRobinTopics) group.Skipped = true;
        }

        return outcome;
    }

    /// <summary>
    /// Confirma con la IA que el tema corresponde al país configurado. Devuelve <c>false</c>
    /// cuando no es relevante o cuando la validación no se pudo completar (fail-closed):
    /// en ambos casos la temática se omite en esta corrida y se registra el motivo.
    /// </summary>
    private async Task<bool> ValidateCountryAsync(
        AutomationRun run, DiscoveredTopic topic, AutomationSettings settings, CancellationToken ct)
    {
        try
        {
            var result = await _countryValidator.ValidateAsync(new TopicCountryCheckRequest
            {
                TopicTitle = topic.Title,
                Snippet = topic.Snippet,
                Source = topic.Source,
                CountryCode = settings.Country,
                Language = settings.Language,
                ApiKey = settings.AiApiKey,
                BaseUrl = settings.AiBaseUrl,
                Model = settings.AiModel
            }, ct);

            AccumulateUsage(run, result.TotalTokens);

            if (result.IsRelevant) return true;

            AddSkippedTopic(run, topic, string.IsNullOrWhiteSpace(result.Reason)
                ? "El tema no corresponde al país configurado."
                : result.Reason.Trim());

            _logger.LogInformation(
                "Tema '{Topic}' omitido: no corresponde al país configurado ({Country}).",
                topic.Title, settings.Country);
            return false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Fail-closed: si no se puede validar, no se redacta el artículo.
            AddSkippedTopic(run, topic, $"No se pudo validar el país: {ex.Message}");

            _logger.LogWarning(ex,
                "No se pudo validar el país del tema '{Topic}'; se omite la temática.", topic.Title);
            return false;
        }
    }

    private static void AddSkippedTopic(AutomationRun run, DiscoveredTopic topic, string reason) =>
        run.SkippedTopics.Add(new AutomationSkippedTopic
        {
            TopicTitle = topic.Title,
            Source = topic.Source,
            BaseTopic = topic.BaseTopic,
            Reason = reason
        });

    private static string BaseTopicKey(DiscoveredTopic topic) =>
        string.IsNullOrWhiteSpace(topic.BaseTopic)
            ? topic.Title.Trim()
            : topic.BaseTopic.Trim();

    /// <summary>
    /// Agrupa los candidatos por temática base. Con round-robin se respeta el orden de
    /// configuración y cada temática recibe su propio turno; sin round-robin se conserva
    /// una única secuencia en el orden descubierto.
    /// </summary>
    private static List<TopicGroup> BuildTopicGroups(
        IReadOnlyList<DiscoveredTopic> topics, IReadOnlyList<string> baseTopics, bool roundRobin)
    {
        if (!roundRobin)
            return [new TopicGroup { Candidates = [.. topics] }];

        var byKey = new Dictionary<string, List<DiscoveredTopic>>(StringComparer.OrdinalIgnoreCase);

        foreach (var topic in topics)
        {
            var key = BaseTopicKey(topic);

            if (!byKey.TryGetValue(key, out var list))
            {
                list = [];
                byKey[key] = list;
            }

            list.Add(topic);
        }

        var groups = new List<TopicGroup>();

        // Primero las temáticas configuradas (aunque Serper no haya devuelto resultados).
        foreach (var baseTopic in baseTopics)
        {
            if (byKey.Remove(baseTopic, out var candidates) && candidates.Count > 0)
                groups.Add(new TopicGroup { Key = baseTopic, Candidates = candidates });
        }

        // Después cualquier grupo extra que no corresponda a una temática configurada.
        foreach (var (key, candidates) in byKey)
        {
            if (candidates.Count > 0)
                groups.Add(new TopicGroup { Key = key, Candidates = candidates });
        }

        return groups;
    }

    private enum TopicOutcome
    {
        Generated,
        Pending,
        Duplicate,
        NotRelevant,
        Failed
    }

    private sealed class TopicGroup
    {
        public string Key { get; init; } = string.Empty;
        public List<DiscoveredTopic> Candidates { get; init; } = [];
        public int Cursor { get; set; }
        public bool HasNext => Cursor < Candidates.Count;

        /// <summary>La temática se omite en la corrida (por país) y no admite más candidatos.</summary>
        public bool Skipped { get; set; }

        public DiscoveredTopic? Next() => HasNext ? Candidates[Cursor++] : null;
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
