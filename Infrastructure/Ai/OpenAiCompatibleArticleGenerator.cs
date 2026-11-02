using System.Net.Http.Json;
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
        _pipeline = ResiliencePolicies.CreateHttpPipeline(TimeSpan.FromSeconds(180));
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
                temperature = Math.Clamp(request.Temperature, 0.0, 1.5),
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
            var parsed = JsonSerializer.Deserialize<AiChatCompletion>(content, JsonDefaults.CaseInsensitive);
            var choice = parsed?.Choices?.FirstOrDefault();
            var reason = choice?.FinishReason;
            var message = choice?.Message?.Content;
            var usage = parsed?.Usage;
            var completion = usage?.CompletionTokens;
            var reasoning = usage?.CompletionDetails?.ReasoningTokens;

            if (string.Equals(reason, "length", StringComparison.OrdinalIgnoreCase))
            {
                var detail = completion is not null
                    ? $" (salida: {completion} tokens" + (reasoning is not null ? $", razonamiento: {reasoning} tokens" : "") + ")"
                    : string.Empty;
                var hint = reasoning is not null && completion is not null && reasoning >= completion
                    ? " El modelo consumió todo el presupuesto en razonamiento; usa un modelo sin razonamiento (p. ej. deepseek-chat) o aumenta 'Máx. tokens/artículo'."
                    : " Aumenta 'Máx. tokens/artículo' o reduce la longitud del artículo.";
                throw new InvalidOperationException($"La respuesta de la IA se truncó por límite de tokens (finish_reason=length{detail}).{hint}");
            }

            if (string.IsNullOrWhiteSpace(message))
                throw new InvalidOperationException($"La IA no devolvió contenido (finish_reason: {reason ?? "desconocido"}).");

            var article = JsonSerializer.Deserialize<AiGeneratedArticle>(message, JsonDefaults.CaseInsensitive)
                ?? throw new InvalidOperationException("La respuesta de la IA no es un artículo válido.");

            return new GeneratedArticle
            {
                Title = article.Title?.Trim() ?? string.Empty,
                Summary = article.Summary?.Trim() ?? string.Empty,
                Blocks = AiBlockMapper.MapBlocks(article.Blocks),
                Tags = (article.Tags ?? []).Select(t => t.Trim()).Where(t => t.Length > 0).ToList(),
                CategorySuggestion = article.CategorySuggestion,
                MetaDescription = article.MetaDescription?.Trim() ?? string.Empty,
                TotalTokens = parsed?.Usage?.TotalTokens
            };
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"La IA devolvió una respuesta que no es JSON válido ({ex.Message}). Revisa el modelo o el presupuesto de tokens.", ex);
        }
    }

    private static string BuildPrompt(TopicGenerationRequest request)
    {
        var context = string.IsNullOrWhiteSpace(request.Snippet) ? string.Empty : $"Contexto: {request.Snippet}\n";
        var source = string.IsNullOrWhiteSpace(request.Source) ? string.Empty : $"Fuente de referencia: {request.Source}\n";
        var length = request.TargetWords > 0 ? $"Extensión objetivo: {request.TargetWords} palabras (aproximada).\n" : string.Empty;

        var minBlocks = Math.Max(3, request.MinBlocks);
        var maxBlocks = Math.Max(minBlocks, request.MaxBlocks);

        var prompt =
            $"Escribe un artículo periodístico en {request.Language} sobre la temática:\n" +
            $"\"{request.TopicTitle}\"\n" +
            context + source + length +
            $"Tono editorial: {DescribeTone(request.Tone)}.\n" +
            $"Estructura: {DescribeTemplate(request.Template)}.\n" +
            $"Usa entre {minBlocks} y {maxBlocks} bloques en total.\n" +
            DescribeRequiredSections(request) +
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
            "Reglas obligatorias:\n" +
            "- El primer bloque debe ser un \"paragraph\" (introducción) y no debe empezar por un \"heading\".\n" +
            "- No uses dos encabezados seguidos; cada \"heading\" debe ir seguido de contenido.\n" +
            "- Nunca uses \"heading\" de nivel 1 (el título ya va en \"title\").\n" +
            "- Los tipos permitidos son \"heading\" (level 2 o 3), \"paragraph\", \"bullet-list\", \"ordered-list\", \"quote\", \"callout\", \"table\", \"divider\" y \"code\". No incluyas imágenes.\n" +
            "- Cada lista debe tener al menos 3 elementos; no dejes bloques vacíos ni tablas sin filas.\n" +
            "- \"summary\": 1 o 2 frases (máx. 220 caracteres). \"metaDescription\": 140-160 caracteres.\n" +
            "- \"tags\": entre 3 y 8 etiquetas cortas, sin el símbolo #.\n" +
            "- \"categorySuggestion\": una sola categoría, en español.\n" +
            "- El cuerpo debe alcanzar la extensión objetivo con párrafos desarrollados, sin relleno ni repeticiones.";

        if (!string.IsNullOrWhiteSpace(request.CorrectionHint))
        {
            prompt +=
                "\n\nEl intento anterior no cumplió la estructura y fue rechazado. Corrige estos puntos en la nueva versión:\n" +
                request.CorrectionHint.Trim();
        }

        return prompt;
    }

    private static string DescribeTemplate(string? template) => template?.Trim().ToLowerInvariant() switch
    {
        "informative" =>
            "artículo informativo. Explica el tema con claridad y de forma completa: qué es, cómo funciona, datos clave, contexto y por qué es relevante, priorizando la comprensión del lector sobre la inmediatez.",
        "analysis" =>
            "artículo de análisis. Abre con el contexto del tema, desarrolla 3 o 4 secciones con causas, datos e implicaciones, y cierra con una valoración o perspectiva.",
        "guide" =>
            "guía práctica. Abre con el problema u objetivo del lector, organiza el contenido en pasos o secciones accionables y cierra con recomendaciones concretas.",
        _ =>
            "noticia. Abre con el hecho principal (qué, quién, cuándo, dónde y por qué), desarrolla los detalles en orden de importancia y cierra con contexto o próximos pasos."
    };

    private static string DescribeTone(string? tone) => tone?.Trim().ToLowerInvariant() switch
    {
        "analitico" or "analítico" => "analítico, riguroso y con interpretación de los hechos",
        "cronica" or "crónica" => "narrativo y descriptivo, con ritmo de crónica",
        "divulgativo" => "divulgativo, claro y cercano, explicando los conceptos técnicos",
        _ => "informativo, claro, objetivo y en tercera persona"
    };

    private static string DescribeRequiredSections(TopicGenerationRequest request)
    {
        var rules = new List<string>();
        if (request.IncludeList) rules.Add("incluye al menos una lista (\"bullet-list\" u \"ordered-list\")");
        if (request.IncludeTable) rules.Add("incluye una \"table\" con encabezados y al menos 2 filas");
        if (request.IncludeQuote) rules.Add("incluye al menos una \"quote\" con atribución");
        if (request.IncludeConclusion) rules.Add("cierra con un \"heading\" de nivel 2 (\"Conclusión\") seguido de un párrafo de cierre");

        return rules.Count == 0
            ? string.Empty
            : $"Secciones requeridas: {string.Join("; ", rules)}.\n";
    }

    private const string SystemPrompt =
        "Eres un redactor editorial de un portal de noticias. Respondes únicamente con JSON válido y sin comentarios.";
}

internal sealed class AiChatCompletion
{
    public List<AiChoice>? Choices { get; set; }
    public AiUsageResponse? Usage { get; set; }
}

internal sealed class AiUsageResponse
{
    [JsonPropertyName("total_tokens")]
    public int? TotalTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int? CompletionTokens { get; set; }

    [JsonPropertyName("completion_tokens_details")]
    public AiCompletionDetails? CompletionDetails { get; set; }
}

internal sealed class AiCompletionDetails
{
    [JsonPropertyName("reasoning_tokens")]
    public int? ReasoningTokens { get; set; }
}

internal sealed class AiChoice
{
    public AiMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

internal sealed class AiMessage
{
    public string? Content { get; set; }
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
    public string? Caption { get; set; }
    public List<string>? Headers { get; set; }
    public List<List<string>>? Rows { get; set; }
    public string? Code { get; set; }
    public string? Language { get; set; }
}
