namespace Aplication.DTOs.Automation;

/// <summary>Temática candidata descubierta por Serper.</summary>
public sealed class DiscoveredTopic
{
    public string Title { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Snippet { get; set; }

    /// <summary>Temática base de la que proviene el candidato (útil para el reparto round-robin).</summary>
    public string? BaseTopic { get; set; }
}
