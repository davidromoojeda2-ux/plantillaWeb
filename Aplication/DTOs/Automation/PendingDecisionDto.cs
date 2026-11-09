using Domine.Entities;

namespace Aplication.DTOs.Automation;

/// <summary>Decisión pendiente sobre una temática repetida o un artículo fuera de estándar.</summary>
public sealed class PendingDecisionDto
{
    public string RunId { get; set; } = string.Empty;
    public string DecisionId { get; set; } = string.Empty;
    public string TopicTitle { get; set; } = string.Empty;
    public string? Source { get; set; }
    public AutomationDecisionKind Kind { get; set; }
    public List<string> ValidationErrors { get; set; } = [];
}
