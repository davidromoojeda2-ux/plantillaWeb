using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Aplication.DTOs.Automation;
using Aplication.Interfaces;
using Domine.Entities;
using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace Infrastructure.Balances;

/// <summary>
/// Consulta el saldo disponible en Serper (`GET /account`) y en la IA.
/// DeepSeek expone `GET /user/balance`; otros proveedores no permiten leer el saldo por API.
/// </summary>
public sealed class ProviderBalanceService : IProviderBalanceService
{
    private readonly HttpClient _http;
    private readonly IAutomationStateRepository _state;
    private readonly ILogger<ProviderBalanceService> _logger;
    private readonly ResiliencePipeline _pipeline;

    public ProviderBalanceService(
        HttpClient http,
        IAutomationStateRepository state,
        ILogger<ProviderBalanceService> logger)
    {
        _http = http;
        _state = state;
        _logger = logger;
        _pipeline = ResiliencePolicies.CreateHttpPipeline();
    }

    public async Task<ProviderBalanceDto> GetBalancesAsync(CancellationToken ct = default)
    {
        var result = new ProviderBalanceDto();
        var settings = await _state.GetSettingsAsync();
        if (settings is null) return result;

        await TrySerperAsync(settings.SerperApiKey, result, ct);
        await TryAiAsync(settings, result, ct);
        return result;
    }

    private async Task TrySerperAsync(string apiKey, ProviderBalanceDto result, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return;

        try
        {
            var json = await _pipeline.ExecuteAsync(async inner =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, "https://google.serper.dev/account");
                req.Headers.TryAddWithoutValidation("X-API-KEY", apiKey);
                var resp = await _http.SendAsync(req, inner);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync(inner);
            }, ct);

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("balance", out var balance) && balance.TryGetInt64(out var credits))
                result.SerperCredits = credits;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo consultar el saldo de Serper.");
        }
    }

    private async Task TryAiAsync(AutomationSettings settings, ProviderBalanceDto result, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(settings.AiApiKey)) return;

        var isDeepSeek = string.Equals(settings.AiProvider, "deepseek", StringComparison.OrdinalIgnoreCase)
                         || settings.AiBaseUrl.Contains("deepseek", StringComparison.OrdinalIgnoreCase);
        if (!isDeepSeek)
        {
            result.AiNote = "El saldo no está disponible vía API para este proveedor; consulta su panel.";
            return;
        }

        try
        {
            var baseUrl = (string.IsNullOrWhiteSpace(settings.AiBaseUrl) ? "https://api.deepseek.com" : settings.AiBaseUrl).TrimEnd('/');
            var json = await _pipeline.ExecuteAsync(async inner =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/user/balance");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AiApiKey);
                var resp = await _http.SendAsync(req, inner);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync(inner);
            }, ct);

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("balance_infos", out var infos) && infos.GetArrayLength() > 0)
            {
                var info = infos[0];
                if (info.TryGetProperty("total_balance", out var total)
                    && decimal.TryParse(total.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var balance))
                {
                    result.AiBalance = balance;
                }

                if (info.TryGetProperty("currency", out var currency))
                    result.AiCurrency = currency.GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo consultar el saldo de la IA.");
        }
    }
}
