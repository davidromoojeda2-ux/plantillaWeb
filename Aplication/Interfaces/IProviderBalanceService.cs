using Aplication.DTOs.Automation;

namespace Aplication.Interfaces;

/// <summary>Consulta el saldo disponible en los proveedores de Serper e IA.</summary>
public interface IProviderBalanceService
{
    Task<ProviderBalanceDto> GetBalancesAsync(CancellationToken ct = default);
}
