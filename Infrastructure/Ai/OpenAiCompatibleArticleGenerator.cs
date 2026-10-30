using System.Net.Http.Json;
using System.Text.Json;
using Aplication.DTOs.Automation;
using Aplication.Interfaces;
using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace Infrastructure.Ai;

/// <summary>
/// Generador de artículos mediante un endpoint compatible con la API de OpenAI
/// (OpenAI, DeepSeek, OpenRouter, Groq, Mistral, Ollama u otro). La clave, la URL base
/// y el modelo se reciben por solicitud, lo que lo hace agnóstico al proveedor.
/// </summary>
public sealed class OpenAiCompatibleArticleGenerator : IAiArticleGenerator
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenAiCompatibleArticleGenerator> _logger;
    private readonly ResiliencePipeline _pipeline;

    public OpenAiCompatibleArticleGenerator(HttpClient http, ILogger<OpenAiCompatibleArticleGenerator> logger)
    {
        _http = http;
        _logger = logger;
        _pipeline = ResiliencePolicies.CreateHttpPipeline();
    }

    public async Task<GeneratedArticle> GenerateAsync(TopicGenerationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new InvalidOperationException("La IA no está configurada (clave ausente).");

        var baseUrl = string.IsNullOrWhiteSpace(request.BaseUrl) ? "https://api.openai.com/v1" : request.BaseUrl.TrimEnd('/');
        var model = string.IsNullOrWhiteSpace(request.Model) ? "gpt-4o-mini" : request.Model;
        var prompt = BuildPrompt(request);

        var content = await _pipeline.ExecuteAsync(async inner =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {request.ApiKey}");
            req.Content = JsonContent.Create(new
            {
                model,
                temperature = 0.7,
                max_tokens = Math.Max(256, request.MaxTokens),
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = prompt }
                }
            });

            var resp = await _http.SendAsync(req, inner);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(inner);
        }, ct);

        return MapResponse(content);
    }

    private GeneratedArticle MapResponse(string content)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<AiChatCompletion>(content, JsonDefaults);
            var choice = parsed?.Choices?.FirstOrDefault();
            var message = choice?.Message?.Content;
            if (string.IsNullOrWhiteSpace(message))
            {
                var reason = choice?.FinishReason;
                if (string.Equals(reason, "length", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("La IA agotó el límite de tokens antes de devolver el artículo. Aumenta 'Máx. tokens por artículo' (los modelos de razonamiento necesitan más).");

                throw new InvalidOperationException($"La IA no devolvió contenido (finish_reason: {reason ?? "desconocido"}).");
            }

            var article = JsonSerializer.Deserialize<AiGeneratedArticle>(message, JsonDefaults)
                ?? throw new InvalidOperationException("La respuesta de la IA no es un artículo válido.");

            return new GeneratedArticle
            {
                Title = article.Title?.Trim() ?? string.Empty,
                Summary = article.Summary?.Trim() ?? string.Empty,
                Blocks = AiBlockMapper.MapBlocks(article.Blocks),
                Tags = (article.Tags ?? []).Select(t => t.Trim()).Where(t => t.Length > 0).ToList(),
                CategorySuggestion = article.CategorySuggestion,
                MetaDescription = article.MetaDescription?.Trim() ?? string.Empty
            };
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("No se pudo interpretar la respuesta de la IA.", ex);
        }
    }

    private static string BuildPrompt(TopicGenerationRequest request)
    {
        var context = string.IsNullOrWhiteSpace(request.Snippet) ? string.Empty : $"Contexto: {request.Snippet}\n";
        var source = string.IsNullOrWhiteSpace(request.Source) ? string.Empty : $"Fuente de referencia: {request.Source}\n";

        var prompt =
            $"Escribe un artículo periodístico en {request.Language} sobre la temática:\n" +
            $"\"{request.TopicTitle}\"\n" +
            context + source +
            "Devuelve SOLO un JSON válido (sin markdown) con esta forma:\n" +
            "{\n" +
            "  \"title\": \"...\",\n" +
            "  \"summary\": \"...\",\n" +
            "  \"metaDescription\": \"...\",\n" +
            "  \"categorySuggestion\": \"...\",\n" +
            "  \"tags\": [\"...\"],\n" +
            "  \"blocks\": [\n" +
            "    { \"type\": \"heading\", \"level\": 2, \"text\": \"...\" },\n" +
            "    { \"type\": \"paragraph\", \"text\": \"...\" },\n" +
            "    { \"type\": \"bullet-list\", \"items\": [\"...\", \"...\"] }\n" +
            "  ]\n" +
            "}\n" +
            "Reglas: usa entre 4 y 8 bloques; los tipos permitidos son \"heading\" (level 2 o 3),\n" +
            "\"paragraph\", \"bullet-list\", \"ordered-list\", \"quote\", \"callout\", \"table\" y \"divider\".\n" +
            "No incluyas imágenes. Escribe como mínimo 4 párrafos.";

        return prompt;
    }

    private const string SystemPrompt =
        "Eres un redactor editorial de un portal de noticias. Respondes únicamente con JSON válido y sin comentarios.";

    private static readonly JsonSerializerOptions JsonDefaults = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

internal sealed class AiChatCompletion
{
    public List<AiChoice>? Choices { get; set; }
}

internal sealed class AiChoice
{
    public AiMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}

internal sealed class AiMessage
{
    public string? Content { get; set; }
    public string? ReasoningContent { get; set; }
}

internal sealed class AiGeneratedArticle
{
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? MetaDescription { get; set; }
    public string? CategorySuggestion { get; set; }
    public List<string>? Tags { get; set; }
    public List<AiBlock>? Blocks { get; set; }
}

internal sealed class AiBlock
{
    public string? Type { get; set; }
    public string? Text { get; set; }
    public int? Level { get; set; }
    public List<string>? Items { get; set; }
    public string? Attribution { get; set; }
    public string? Variant { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Alt { get; set; }
    public string? Caption { get; set; }
    public string? Credit { get; set; }
    public List<string>? Headers { get; set; }
    public List<List<string>>? Rows { get; set; }
    public string? Code { get; set; }
    public string? Language { get; set; }
}
