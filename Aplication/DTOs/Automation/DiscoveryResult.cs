namespace Aplication.DTOs.Automation;

/// <summary>Resultado del descubrimiento: temáticas candidatas y créditos consumidos en Serper.</summary>
public sealed class DiscoveryResult
{
    public List<DiscoveredTopic> Topics { get; set; } = [];
    public int Credits { get; set; }
}
