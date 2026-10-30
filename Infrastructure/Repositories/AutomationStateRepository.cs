using Aplication.Interfaces;
using Domine.Entities;
using Infrastructure.Persistence;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

public class AutomationStateRepository : IAutomationStateRepository
{
    private readonly IMongoCollection<AutomationRun> _runs;
    private readonly IMongoCollection<AutomationSettings> _settings;

    public AutomationStateRepository(MongoDbContext ctx)
    {
        _runs = ctx.AutomationRuns;
        _settings = ctx.AutomationSettings;
    }

    public async Task<AutomationSettings?> GetSettingsAsync() =>
        await _settings.Find(_ => true).FirstOrDefaultAsync();

    public async Task SaveSettingsAsync(AutomationSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        var existing = await _settings.Find(s => s.Id == settings.Id).FirstOrDefaultAsync();
        if (existing is null)
            await _settings.InsertOneAsync(settings);
        else
            await _settings.ReplaceOneAsync(s => s.Id == settings.Id, settings);
    }

    public Task InsertRunAsync(AutomationRun run) => _runs.InsertOneAsync(run);

    public Task UpdateRunAsync(AutomationRun run) =>
        _runs.ReplaceOneAsync(r => r.Id == run.Id, run);

    public async Task<List<AutomationRun>> GetRunsAsync(int skip = 0, int take = 20) =>
        await _runs.Find(_ => true)
                   .SortByDescending(r => r.CreatedAt)
                   .Skip(skip).Limit(take).ToListAsync();

    public async Task<AutomationRun?> GetRunAsync(string id) =>
        await _runs.Find(r => r.Id == id).FirstOrDefaultAsync();

    public async Task<bool> HashExistsAsync(string hash)
    {
        var filter = Builders<AutomationRun>.Filter.AnyEq(r => r.TopicHashes, hash);
        return await _runs.CountDocumentsAsync(filter) > 0;
    }

    public async Task<List<(AutomationRun Run, AutomationDecision Decision)>> GetPendingDecisionsAsync()
    {
        var filter = Builders<AutomationRun>.Filter.ElemMatch(
            r => r.PendingDecisions, d => d.Status == AutomationDecisionStatus.Pendiente);

        var runs = await _runs.Find(filter)
                              .SortByDescending(r => r.CreatedAt)
                              .Limit(100).ToListAsync();

        var result = new List<(AutomationRun, AutomationDecision)>();
        foreach (var run in runs)
        {
            foreach (var decision in run.PendingDecisions.Where(d => d.Status == AutomationDecisionStatus.Pendiente))
                result.Add((run, decision));
        }

        return result;
    }

    public async Task ResolveDecisionAsync(string runId, string decisionId, AutomationDecisionStatus status)
    {
        // Se carga y reemplaza el documento completo para evitar el operador posicional
        // (no soportado con índices negativos en el driver actual).
        var run = await _runs.Find(r => r.Id == runId).FirstOrDefaultAsync();
        var decision = run?.PendingDecisions.FirstOrDefault(d => d.Id == decisionId);
        if (run is null || decision is null) return;

        decision.Status = status;
        await _runs.ReplaceOneAsync(r => r.Id == runId, run);
    }
}
