namespace Aplication.DTOs.Automation;

/// <summary>Temática candidata descubierta por Serper.</summary>
public sealed class DiscoveredTopic
{
    public string Title { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Link { get; set; }
    public string? Snippet { get; set; }
}
