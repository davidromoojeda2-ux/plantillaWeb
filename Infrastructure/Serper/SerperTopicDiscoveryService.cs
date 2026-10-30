using System.Net.Http.Json;
using System.Text.Json;
using Aplication.DTOs.Automation;
using Aplication.Interfaces;
using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace Infrastructure.Serper;

/// <summary>
/// Descubrimiento de temáticas mediante el endpoint de noticias de Serper (api.serper.dev).
/// La clave se recibe por solicitud (gestionada desde el panel de configuración).
/// </summary>
public sealed class SerperTopicDiscoveryService : ITopicDiscoveryService
{
    private const string BaseUrl = "https://google.serper.dev";

    private readonly HttpClient _http;
    private readonly ILogger<SerperTopicDiscoveryService> _logger;
    private readonly ResiliencePipeline _pipeline;

    public SerperTopicDiscoveryService(HttpClient http, ILogger<SerperTopicDiscoveryService> logger)
    {
        _http = http;
        _logger = logger;
        _http.BaseAddress = new Uri(BaseUrl);
        _pipeline = ResiliencePolicies.CreateHttpPipeline();
    }

    public async Task<List<DiscoveredTopic>> DiscoverAsync(TopicDiscoveryRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            _logger.LogWarning("Serper no está configurado (ApiKey ausente). No se descubrirán temáticas.");
            return [];
        }

        if (request.Topics is null || request.Topics.Count == 0) return [];

        var path = string.Equals(request.SearchType, "search", StringComparison.OrdinalIgnoreCase) ? "/search" : "/news";
        var results = new List<DiscoveredTopic>();
        foreach (var query in request.Topics)
        {
            if (string.IsNullOrWhiteSpace(query)) continue;

            var json = await _pipeline.ExecuteAsync(async inner =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, path);
                req.Headers.TryAddWithoutValidation("X-API-KEY", request.ApiKey);
                req.Content = JsonContent.Create(new
                {
                    q = query.Trim(),
                    gl = request.Country,
                    hl = request.Language,
                    num = Math.Max(1, request.Num),
                    page = 1,
                    tbs = request.Recency,
                    safe = request.Safe,
                    autocorrect = request.Autocorrect
                });

                var resp = await _http.SendAsync(req, inner);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync(inner);
            }, ct);

            results.AddRange(ParseItems(json));
        }

        return results
            .Where(t => !string.IsNullOrWhiteSpace(t.Title))
            .DistinctBy(t => t.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, request.Limit))
            .ToList();
    }

    private static List<DiscoveredTopic> ParseItems(string json)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<SerperNewsResponse>(json, JsonDefaults);
            var items = payload?.News is { Count: > 0 } news ? news : payload?.Organic ?? [];

            return items
                .Select(n => new DiscoveredTopic
                {
                    Title = n.Title ?? string.Empty,
                    Source = n.Source,
                    Link = n.Link,
                    Snippet = n.Snippet
                })
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static readonly JsonSerializerOptions JsonDefaults = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

internal sealed class SerperNewsResponse
{
    public List<SerperNewsItem> News { get; set; } = [];
    public List<SerperNewsItem> Organic { get; set; } = [];
}

internal sealed class SerperNewsItem
{
    public string? Title { get; set; }
    public string? Link { get; set; }
    public string? Snippet { get; set; }
    public string? Source { get; set; }
}
