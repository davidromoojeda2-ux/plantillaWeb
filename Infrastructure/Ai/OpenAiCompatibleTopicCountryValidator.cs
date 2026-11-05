using System.Text.Json;
using System.Text.Json.Serialization;
using Aplication.DTOs.Automation;
using Aplication.Interfaces;
using Infrastructure.Resilience;
using Infrastructure.Serialization;
using Microsoft.Extensions.Logging;
using Polly;

namespace Infrastructure.Ai;

/// <summary>
/// Comprueba con el proveedor de IA si un tema descubierto por Serper corresponde al
/// país configurado. La comprobación es una llamada corta que devuelve un JSON con
/// "relevante" y un motivo breve.
/// </summary>
public sealed class OpenAiCompatibleTopicCountryValidator : ITopicCountryValidator
{
    private const string SystemPrompt =
        "Eres un editor que evalúa la relevancia geográfica de temas periodísticos. " +
        "Respondes únicamente con JSON válido, sin markdown ni comentarios.";

    private readonly HttpClient _http;
    private readonly ILogger<OpenAiCompatibleTopicCountryValidator> _logger;
    private readonly ResiliencePipeline _pipeline;

    public OpenAiCompatibleTopicCountryValidator(
        HttpClient http, ILogger<OpenAiCompatibleTopicCountryValidator> logger)
    {
        _http = http;
        _logger = logger;
        _pipeline = ResiliencePolicies.CreateHttpPipeline(TimeSpan.FromSeconds(60));
    }

    public async Task<TopicCountryCheckResult> ValidateAsync(
        TopicCountryCheckRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new InvalidOperationException("La IA no está configurada (clave ausente).");

        var baseUrl = string.IsNullOrWhiteSpace(request.BaseUrl)
            ? "https://api.openai.com/v1"
            : request.BaseUrl.TrimEnd('/');
        var model = string.IsNullOrWhiteSpace(request.Model) ? "gpt-4o-mini" : request.Model;
        var countryName = LocaleOptions.DescribeCountry(request.CountryCode);

        var content = await OpenAiCompatibleArticleGenerator.SendChatAsync(
            _http, _pipeline, baseUrl, request.ApiKey, model,
            0, 128, SystemPrompt, BuildPrompt(request, countryName), ct);

        var parsed = JsonSerializer.Deserialize<AiChatCompletion>(content, JsonDefaults.CaseInsensitive);
        var message = parsed?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(message))
            throw new InvalidOperationException("La IA no devolvió una respuesta para la validación de país.");

        var payload = JsonSerializer.Deserialize<CountryCheckPayload>(message, JsonDefaults.CaseInsensitive)
            ?? throw new InvalidOperationException("La IA devolvió una respuesta inválida para la validación de país.");

        var isRelevant = ParseRelevant(payload.Relevante);
        _logger.LogDebug(
            "Validación de país para '{Topic}' ({Country}): {Result}.",
            request.TopicTitle, countryName, isRelevant ? "relevante" : "descartado");

        return new TopicCountryCheckResult(
            isRelevant,
            payload.Motivo?.Trim(),
            parsed?.Usage?.TotalTokens);
    }

    private static string BuildPrompt(TopicCountryCheckRequest request, string countryName)
    {
        var source = string.IsNullOrWhiteSpace(request.Source) ? "No indicada" : request.Source.Trim();
        var snippet = string.IsNullOrWhiteSpace(request.Snippet) ? "No disponible" : request.Snippet.Trim();
        var language = OpenAiCompatibleArticleGenerator.DescribeLanguage(request.Language);

        return
            $"País objetivo: {countryName} ({request.CountryCode}).\n" +
            $"Tema: \"{request.TopicTitle}\"\n" +
            $"Fuente: {source}\n" +
            $"Resumen: {snippet}\n" +
            "Determina si el tema es relevante para el país objetivo. " +
            "Marca false solo si el tema pertenece claramente a otro país y no tiene relación con el país objetivo. " +
            "Los temas globales, internacionales o sin una ubicación clara son relevantes (true). " +
            $"Escribe el motivo en {language}.\n" +
            "Devuelve SOLO este JSON: {\"relevante\": true|false, \"motivo\": \"una frase breve\"}";
    }

    private static bool ParseRelevant(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => ParseText(element.GetString()),
        JsonValueKind.Number when element.TryGetInt32(out var number) => number != 0,
        _ => false
    };

    private static bool ParseText(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "true" or "si" or "sí" or "yes" or "1" => true,
            "false" or "no" or "0" => false,
            _ => false
        };
    }
}

internal sealed class CountryCheckPayload
{
    [JsonPropertyName("relevante")]
    public JsonElement Relevante { get; set; }

    [JsonPropertyName("motivo")]
    public string? Motivo { get; set; }
}
