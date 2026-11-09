using Domine.Entities;

namespace Aplication.Interfaces;

/// <summary>Acceso al estado y configuración de la automatización.</summary>
public interface IAutomationStateRepository
{
    Task<AutomationSettings?> GetSettingsAsync();
    Task SaveSettingsAsync(AutomationSettings settings);
    Task InsertRunAsync(AutomationRun run);
    Task UpdateRunAsync(AutomationRun run);
    Task<List<AutomationRun>> GetRunsAsync(int skip = 0, int take = 20);
    Task<AutomationRun?> GetRunAsync(string id);

    /// <summary>Indica si el hash normalizado de un título ya fue procesado en alguna corrida.</summary>
    Task<bool> HashExistsAsync(string hash);

    Task<List<(AutomationRun Run, AutomationDecision Decision)>> GetPendingDecisionsAsync();
    Task<int> GetPendingDecisionCountAsync();

    /// <summary>Totales acumulados de tokens y créditos de Serper.</summary>
    Task<(long TotalTokens, long TotalCredits, long RunCount)> GetUsageTotalsAsync();
}
