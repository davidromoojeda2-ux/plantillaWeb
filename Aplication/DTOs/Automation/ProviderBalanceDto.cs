namespace Aplication.DTOs.Automation;

/// <summary>Saldo disponible en los proveedores (Serper e IA).</summary>
public sealed class ProviderBalanceDto
{
    /// <summary>Créditos disponibles en Serper.</summary>
    public long? SerperCredits { get; set; }

    /// <summary>Saldo disponible de IA.</summary>
    public decimal? AiBalance { get; set; }

    public string? AiCurrency { get; set; }

    /// <summary>Nota cuando el proveedor no expone saldo vía API.</summary>
    public string? AiNote { get; set; }
}
