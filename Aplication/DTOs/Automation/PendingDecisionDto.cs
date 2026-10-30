namespace Aplication.DTOs.Automation;

/// <summary>Decisión pendiente sobre una temática repetida, para el panel administrativo.</summary>
public sealed class PendingDecisionDto
{
    public string RunId { get; set; } = string.Empty;
    public string DecisionId { get; set; } = string.Empty;
    public string TopicTitle { get; set; } = string.Empty;
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; }
}
