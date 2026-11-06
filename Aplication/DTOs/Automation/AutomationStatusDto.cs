namespace Aplication.DTOs.Automation;

using Aplication.Realtime;

/// <summary>Estado consolidado de la automatización para el panel administrativo.</summary>
public sealed class AutomationStatusDto
{
    public bool Paused { get; set; }

    /// <summary>Indica si hay una corrida en curso (o solicitada) para la barra de estado.</summary>
    public bool IsRunning { get; set; }

    public bool AutoPublish { get; set; }
    public int FrequencyMinutes { get; set; }
    public int PendingDecisionsCount { get; set; }
    public AutomationRunSummary? LastRun { get; set; }
    public UsageTotalsDto Usage { get; set; } = new();

    /// <summary>Estado en memoria de la corrida en curso o de la última finalizada.</summary>
    public AutomationLiveSnapshot? Live { get; set; }
}

/// <summary>Resumen de una corrida para la barra de estado.</summary>
public sealed class AutomationRunSummary
{
    public string Status { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public int GeneratedCount { get; set; }
    public int TotalTokens { get; set; }
}

/// <summary>Consumo acumulado de tokens de IA y créditos de Serper.</summary>
public sealed class UsageTotalsDto
{
    public long TotalTokens { get; set; }
    public long TotalCredits { get; set; }
    public long RunCount { get; set; }
}
