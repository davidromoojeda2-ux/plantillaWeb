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

        var content = await SendChatAsync(_http, _pipeline, baseUrl, request.ApiKey, model,
            request.Temperature, request.MaxTokens, SystemPrompt, prompt, ct);

        _logger.LogDebug("Artículo generado para la temática '{Topic}'.", request.TopicTitle);
        return MapResponse(content);
    }

    /// <summary>Envía una petición de chat compatible con OpenAI y devuelve el contenido crudo.</summary>
    internal static async Task<string> SendChatAsync(
        HttpClient http, ResiliencePipeline pipeline, string baseUrl, string apiKey,
        string model, double temperature, int maxTokens, string systemPrompt, string userPrompt,
        CancellationToken ct)
    {
        return await pipeline.ExecuteAsync(async inner =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
            req.Content = JsonContent.Create(new
            {
                model,
                temperature = Math.Clamp(temperature, 0.0, 1.5),
                max_tokens = Math.Max(256, maxTokens),
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            });

            var resp = await http.SendAsync(req, inner);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(inner);
        }, ct);
    }

    internal static GeneratedArticle MapResponse(string content)
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
        var language = DescribeLanguage(request.Language);
        var context = string.IsNullOrWhiteSpace(request.Snippet) ? string.Empty : $"Contexto: {request.Snippet}\n";
        var source = string.IsNullOrWhiteSpace(request.Source) ? string.Empty : $"Fuente de referencia: {request.Source}\n";
        var length = request.TargetWords > 0 ? $"Extensión objetivo: {request.TargetWords} palabras (aproximada).\n" : string.Empty;

        var minBlocks = Math.Max(3, request.MinBlocks);
        var keyword = string.IsNullOrWhiteSpace(request.PrimaryKeyword)
            ? string.Empty
            : $"Palabra clave principal: {request.PrimaryKeyword}. Optimiza el artículo para ella (densidad natural 1-2 %, sin keyword stuffing) e incluye variaciones y sinónimos en los encabezados.\n";

        var prompt =
            $"Escribe un artículo periodístico en {language} sobre la temática:\n" +
            $"\"{request.TopicTitle}\"\n" +
            context + source + length + keyword +
            $"Idioma de salida obligatorio: {language}. Escribe todo el contenido visible (título, resumen, meta descripción, bloques y etiquetas) íntegramente en {language}, sin mezclar idiomas.\n" +
            $"Tono editorial: {DescribeTone(request.Tone)}\n" +
            $"Estructura: {DescribeTemplate(request.Template)}.\n" +
            $"Usa al menos {minBlocks} bloques en total (sin máximo).\n" +
            DescribeRequiredSections(request) +
            DescribeForbiddenSections(request.IncludeConclusion, request.IncludeFaq, request.IncludeCta, request.Template) +
            DescribeInternalLinks(request) +
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
            "- \"categorySuggestion\": una sola categoría del portal (nombre en español) para clasificar el artículo; el contenido visible y las etiquetas van en el idioma indicado.\n" +
            "- El contenido debe ser 100% original: no copies ni parafrasees literalmente otras fuentes; redacta con voz propia y atribuye los datos, cifras y citas a su fuente.\n" +
            "- El cuerpo debe alcanzar la extensión objetivo con párrafos desarrollados, sin relleno ni repeticiones.";

        if (!string.IsNullOrWhiteSpace(request.CorrectionHint))
        {
            prompt +=
                "\n\nEl intento anterior no cumplió la estructura y fue rechazado. Corrige estos puntos en la nueva versión:\n" +
                request.CorrectionHint.Trim();
        }

        return prompt;
    }

    internal static string DescribeTemplate(string? template) => NormalizeTemplate(template) switch
    {
        "informative" =>
            "Artículo informativo con enfoque explicativo.\n" +
            "- Introducción que responda qué es el tema y por qué importa.\n" +
            "- Secciones con encabezados temáticos (nivel 2) sobre cómo funciona, datos clave, contexto y aplicaciones.\n" +
            "- Ejemplos concretos y definiciones claras; prioriza la comprensión sobre la inmediatez.\n" +
            "- No organices el texto como una noticia cronológica ni como una guía de pasos.",
        "comparative" =>
            "Artículo comparativo entre opciones o alternativas.\n" +
            "- Introducción con las opciones comparadas y los criterios elegidos.\n" +
            "- Compara las opciones con criterios explícitos y homogéneos.\n" +
            "- Secciones por criterio (características, rendimiento, precio, facilidad de uso, etc.).\n" +
            "- Resume ventajas y desventajas para cada opción.\n" +
            "- Incluye una recomendación razonada según el perfil del lector, integrada en el cuerpo.",
        "analysis" =>
            "Artículo de análisis e interpretación.\n" +
            "- Introducción con el contexto y la pregunta central.\n" +
            "- Secciones que desarrollen causas, evidencia o datos e implicaciones.\n" +
            "- Contrapuntos, riesgos y escenarios posibles.\n" +
            "- Interpretación propia sustentada en los datos; evita limitarte a resumir la noticia.",
        "guide" =>
            "Guía práctica orientada a la acción.\n" +
            "- Introducción con el problema u objetivo del lector.\n" +
            "- Requisitos, herramientas o preparación previa.\n" +
            "- Pasos o secciones accionables con instrucciones claras.\n" +
            "- Errores comunes y recomendaciones prácticas dentro del cuerpo.\n" +
            "- No la escribas como una noticia ni como un análisis teórico.",
        "faq" =>
            "Artículo de preguntas frecuentes.\n" +
            "- Introducción breve de un solo párrafo.\n" +
            "- De 4 a 6 preguntas como \"heading\" de nivel 3.\n" +
            "- Cada pregunta debe ir seguida de una respuesta directa de 70 a 120 palabras.\n" +
            "- No agregues secciones ajenas al formato de pregunta y respuesta.",
        "ppa" =>
            "Respuesta tipo \"People Also Ask\".\n" +
            "- Abre con 1 o 2 párrafos que respondan directamente a la pregunta principal.\n" +
            "- Después, 3 bloques de pregunta con \"heading\" de nivel 3.\n" +
            "- Cada bloque lleva un párrafo explicativo y 3 pasos o claves.\n" +
            "- No uses una estructura de noticia ni de guía extensa.",
        "job" =>
            "Convocatoria laboral.\n" +
            "- Presenta la empresa y el contexto de la búsqueda.\n" +
            "- Incluye 6 a 8 vacantes como \"heading\" de nivel 3.\n" +
            "- Para cada vacante, detalla requisitos, funciones y beneficios.\n" +
            "- Añade el perfil buscado y consejos de postulación.\n" +
            "- No agregues secciones genéricas de cierre ni de preguntas frecuentes.",
        _ =>
            "Noticia periodística.\n" +
            "- Primer párrafo con el hecho principal (qué, quién, cuándo, dónde y por qué).\n" +
            "- Párrafos siguientes en orden de importancia decreciente: datos, declaraciones y reacciones.\n" +
            "- Antecedentes y contexto dentro del cuerpo, según aporten.\n" +
            "- No uses secciones de opinión ni una estructura de guía."
    };

    internal static string NormalizeTemplate(string? template)
    {
        var normalized = template?.Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(normalized) ? "news" : normalized;
    }

    /// <summary>
    /// Refuerza que las secciones opcionales solo se incluyan cuando el usuario las marcó.
    /// La plantilla FAQ/PPA ya define su propio formato de preguntas y respuestas.
    /// </summary>
    internal static string DescribeForbiddenSections(
        bool includeConclusion, bool includeFaq, bool includeCta, string? template)
    {
        var normalizedTemplate = NormalizeTemplate(template);
        var forbidden = new List<string>();

        if (!includeConclusion) forbidden.Add("una sección de \"Conclusión\"");
        if (!includeFaq && normalizedTemplate is not ("faq" or "ppa"))
            forbidden.Add("una sección de \"Preguntas frecuentes\"");
        if (!includeCta) forbidden.Add("una llamada a la acción");

        return forbidden.Count == 0
            ? string.Empty
            : $"No agregues {string.Join(" ni ", forbidden)} si no se solicitan.\n";
    }

    internal static string DescribeLanguage(string? code)
    {
        var normalized = (code ?? string.Empty).Trim();
        var match = LocaleOptions.Languages.FirstOrDefault(l =>
            string.Equals(l.Code, normalized, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(match.Name)) return match.Name;
        return string.IsNullOrWhiteSpace(normalized) ? "Español" : normalized;
    }

    internal static string DescribeTone(string? tone) => tone?.Trim().ToLowerInvariant() switch
    {
        "analitico" or "analítico" =>
            "Tono analítico y riguroso.\n" +
            "- Examina causas, evidencia e implicaciones; explica el porqué de los hechos.\n" +
            "- Usa conectores lógicos (sin embargo, por tanto, en consecuencia) y matiza las afirmaciones.\n" +
            "- Diferencia hechos de interpretaciones y señala supuestos o limitaciones.\n" +
            "- Evita frases grandilocuentes y conclusiones sin respaldo.",
        "cronica" or "crónica" =>
            "Tono narrativo de crónica.\n" +
            "- Cuenta los hechos con ritmo, escenas y detalles concretos.\n" +
            "- Combina párrafos descriptivos con testimonios y momentos clave.\n" +
            "- Usa un lenguaje literario y fluido, sin caer en ficción ni inventar escenas.\n" +
            "- Mantén la veracidad: todo detalle narrativo debe provenir de la información disponible.",
        "divulgativo" =>
            "Tono divulgativo y didáctico.\n" +
            "- Explica los conceptos técnicos con analogías y ejemplos cotidianos.\n" +
            "- Usa frases cortas y definiciones sencillas antes de profundizar.\n" +
            "- Anticipa las dudas del lector y respóndelas con claridad.\n" +
            "- Evita la jerga innecesaria; si usas un término técnico, explícalo.",
        "amigable" or "conversacional" =>
            "Tono cercano y conversacional.\n" +
            "- Habla directamente al lector con \"tú\" y preguntas retóricas ocasionales.\n" +
            "- Usa un lenguaje natural, cálido y optimista, con ejemplos cercanos.\n" +
            "- Alterna frases cortas con explicaciones sencillas.\n" +
            "- Evita la informalidad excesiva, los chistes forzados y las promesas exageradas.",
        _ =>
            "Tono informativo, objetivo y neutro.\n" +
            "- Escribe en tercera persona y en presente o pasado según el hecho.\n" +
            "- Usa frases claras y directas, sin opiniones ni adjetivos valorativos.\n" +
            "- Prioriza datos verificables, atribuye declaraciones y evita el lenguaje promocional.\n" +
            "- No interpeles al lector ni uses humor."
    };

    private static string DescribeInternalLinks(TopicGenerationRequest request)
    {
        if (!request.IncludeLinks || request.InternalLinks.Count == 0) return string.Empty;

        var list = string.Join("\n", request.InternalLinks.Select(l => $"- {l.Title} → /articulos/{l.Slug}"));
        return $"Enlaces internos disponibles (usa al menos 2 con <a href=\"/articulos/slug\">texto ancla</a>):\n{list}\n";
    }

    private static string DescribeRequiredSections(TopicGenerationRequest request)
    {
        var rules = new List<string>();
        if (request.IncludeList) rules.Add("incluye al menos una lista (\"bullet-list\" u \"ordered-list\")");
        if (request.IncludeTable) rules.Add("incluye una \"table\" con encabezados y al menos 2 filas");
        if (request.IncludeQuote) rules.Add("incluye al menos una \"quote\" con atribución");
        if (request.IncludeConclusion) rules.Add("cierra con un \"heading\" de nivel 2 (\"Conclusión\") seguido de un párrafo de cierre");
        if (request.IncludeFaq) rules.Add("incluye una sección \"Preguntas frecuentes\" (\"heading\" de nivel 2 seguido de 2-4 pares de \"heading\" de nivel 3 con la pregunta y \"paragraph\" con la respuesta)");
        if (request.IncludeCta) rules.Add("cierra con un \"callout\" (variante \"success\" o \"tip\") con una llamada a la acción");
        if (request.IncludeLinks) rules.Add("incluye enlaces de referencia: de 2 a 5 internos usando las rutas \"/articulos/{slug}\" de la lista de enlaces internos disponibles, y de 1 a 3 externos a fuentes autorizadas, con etiquetas <a href=\"...\"> y texto ancla natural");

        return rules.Count == 0
            ? string.Empty
            : $"Secciones requeridas: {string.Join("; ", rules)}.\n";
    }

    private const string SystemPrompt =
        "Eres un redactor editorial senior de un portal de noticias. Redactas siempre en el idioma que se te indique y respondes únicamente con JSON válido y sin comentarios.";
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
