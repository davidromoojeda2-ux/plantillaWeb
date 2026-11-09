using Domine.Entities;

namespace Aplication.Realtime;

/// <summary>Foto inmutable del estado de la corrida en curso o de la última finalizada.</summary>
public sealed record AutomationLiveSnapshot(
    bool IsRunning,
    string? RunId,
    string Trigger,
    string Status,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    int GeneratedCount,
    int PendingCount,
    int DiscoveredTopicsCount,
    int TotalTokens,
    string? LastError)
{
    public static AutomationLiveSnapshot Empty { get; } =
        new(false, null, string.Empty, string.Empty, null, null, 0, 0, 0, 0, null);
}

/// <summary>
/// Estado en memoria de la automatización para que la UI no dependa de persistir
/// la corrida completa en cada artículo o decisión. Es seguro para uso concurrente.
/// </summary>
public sealed class AutomationLiveState
{
    private readonly object _sync = new();
    private AutomationLiveSnapshot _snapshot = AutomationLiveSnapshot.Empty;

    public AutomationLiveSnapshot Snapshot()
    {
        lock (_sync) return _snapshot;
    }

    public void Begin(AutomationRun run)
    {
        lock (_sync) _snapshot = FromRun(run);
    }

    public void Sync(AutomationRun run)
    {
        lock (_sync) _snapshot = FromRun(run);
    }

    public void Finish(AutomationRun run)
    {
        lock (_sync) _snapshot = FromRun(run);
    }

    public void Clear()
    {
        lock (_sync) _snapshot = AutomationLiveSnapshot.Empty;
    }

    private static AutomationLiveSnapshot FromRun(AutomationRun run) =>
        new(
            run.Status == AutomationRunStatus.EnProgreso,
            run.Id,
            run.Trigger.ToString(),
            run.Status.ToString(),
            run.StartedAt,
            run.FinishedAt,
            run.GeneratedArticles.Count,
            run.PendingDecisions.Count(d => d.Status == AutomationDecisionStatus.Pendiente),
            run.DiscoveredTopicsCount,
            run.TotalTokens,
            run.Errors.LastOrDefault());
}
