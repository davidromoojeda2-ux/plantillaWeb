using System.Net.Http.Json;
using System.Text.Json;
using Aplication.DTOs.Automation;
using Aplication.Interfaces;
using Infrastructure.Resilience;
using Infrastructure.Serialization;
using Microsoft.Extensions.Logging;
using Polly;

namespace Infrastructure.Serper;

/// <summary>
/// Descubrimiento de temáticas mediante el endpoint de noticias/búsqueda de Serper (api.serper.dev).
/// La clave y los parámetros se reciben por solicitud (gestionados desde el panel).
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

    public async Task<DiscoveryResult> DiscoverAsync(TopicDiscoveryRequest request, CancellationToken ct = default)
    {
        var result = new DiscoveryResult();

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            _logger.LogWarning("Serper no está configurado (ApiKey ausente). No se descubrirán temáticas.");
            return result;
        }

        if (request.Topics is null || request.Topics.Count == 0) return result;

        var path = string.Equals(request.SearchType, "search", StringComparison.OrdinalIgnoreCase) ? "/search" : "/news";
        var perQuery = new List<List<DiscoveredTopic>>();
        var credits = 0;

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

            var payload = TryParse(json);
            if (payload is null) continue;

            credits += payload.Credits;
            var items = (payload.News is { Count: > 0 } news ? news : payload.Organic)
                .Select(n => new DiscoveredTopic
                {
                    Title = n.Title ?? string.Empty,
                    Source = n.Source,
                    Snippet = n.Snippet
                })
                .ToList();
            perQuery.Add(items);
        }

        var combined = request.RoundRobin ? Interleave(perQuery) : perQuery.SelectMany(x => x);
        result.Topics = combined
            .Where(t => !string.IsNullOrWhiteSpace(t.Title))
            .DistinctBy(t => t.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, request.Limit))
            .ToList();
        result.Credits = credits;

        return result;
    }

    /// <summary>Intercala los resultados de cada consulta (1.º de cada una, 2.º de cada una, …).</summary>
    private static IEnumerable<DiscoveredTopic> Interleave(List<List<DiscoveredTopic>> perQuery)
    {
        var max = perQuery.Count == 0 ? 0 : perQuery.Max(list => list.Count);
        for (var i = 0; i < max; i++)
        {
            foreach (var list in perQuery)
            {
                if (i < list.Count)
                    yield return list[i];
            }
        }
    }

    private static SerperNewsResponse? TryParse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SerperNewsResponse>(json, JsonDefaults.CaseInsensitive);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

internal sealed class SerperNewsResponse
{
    public List<SerperNewsItem> News { get; set; } = [];
    public List<SerperNewsItem> Organic { get; set; } = [];

    /// <summary>Créditos consumidos por la consulta (Serper los informa en la respuesta).</summary>
    public int Credits { get; set; }
}

internal sealed class SerperNewsItem
{
    public string? Title { get; set; }
    public string? Snippet { get; set; }
    public string? Source { get; set; }
}
