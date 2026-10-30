namespace Aplication.DTOs.Automation;

/// <summary>Estado consolidado de la automatización para el panel administrativo.</summary>
public sealed class AutomationStatusDto
{
    public bool Enabled { get; set; }
    public bool Paused { get; set; }

    /// <summary>Indica si hay una corrida en curso (o solicitada) para la barra de estado.</summary>
    public bool IsRunning { get; set; }

    public bool AutoPublish { get; set; }
    public int FrequencyMinutes { get; set; }
    public int PendingDecisionsCount { get; set; }
    public AutomationRunSummary? LastRun { get; set; }
}

/// <summary>Resumen de una corrida para el panel.</summary>
public sealed class AutomationRunSummary
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public int DiscoveredTopicsCount { get; set; }
    public int GeneratedCount { get; set; }
}
