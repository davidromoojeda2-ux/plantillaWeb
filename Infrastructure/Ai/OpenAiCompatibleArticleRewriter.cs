using System.Text;
using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace Infrastructure.Ai;

/// <summary>
/// Reescribe un artículo existente mediante un endpoint compatible con OpenAI,
/// manteniendo el contrato de bloques del portal.
/// </summary>
public sealed class OpenAiCompatibleArticleRewriter : IAiArticleRewriter
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenAiCompatibleArticleRewriter> _logger;
    private readonly ResiliencePipeline _pipeline;

    public OpenAiCompatibleArticleRewriter(HttpClient http, ILogger<OpenAiCompatibleArticleRewriter> logger)
    {
        _http = http;
        _logger = logger;
        _pipeline = ResiliencePolicies.CreateHttpPipeline(TimeSpan.FromSeconds(180));
    }

    public async Task<GeneratedArticle> RewriteAsync(ArticleRewriteRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new InvalidOperationException("La IA no está configurada (clave ausente).");

        var baseUrl = string.IsNullOrWhiteSpace(request.BaseUrl) ? "https://api.openai.com/v1" : request.BaseUrl.TrimEnd('/');
        var model = string.IsNullOrWhiteSpace(request.Model) ? "gpt-4o-mini" : request.Model;
        var prompt = BuildPrompt(request);

        var content = await OpenAiCompatibleArticleGenerator.SendChatAsync(
            _http, _pipeline, baseUrl, request.ApiKey, model,
            request.Temperature, request.MaxTokens, SystemPrompt, prompt, ct);

        _logger.LogDebug("Artículo reescrito a partir de '{Title}'.", request.Title);
        return OpenAiCompatibleArticleGenerator.MapResponse(content);
    }

    private static string BuildPrompt(ArticleRewriteRequest request)
    {
        var language = OpenAiCompatibleArticleGenerator.DescribeLanguage(request.Language);
        var source = RenderBlocks(request.Blocks);
        var length = request.TargetWords > 0 ? $"Extensión objetivo: {request.TargetWords} palabras (aproximada).\n" : string.Empty;
        var topic = string.IsNullOrWhiteSpace(request.Title) ? string.Empty : $"Tema/palabra clave de referencia: {request.Title}.\n";

        var rules = new List<string>();
        if (request.IncludeList) rules.Add("al menos una lista (\"bullet-list\" u \"ordered-list\")");
        if (request.IncludeTable) rules.Add("una \"table\" con encabezados y al menos 2 filas");
        if (request.IncludeQuote) rules.Add("una \"quote\" con atribución");
        if (request.IncludeConclusion) rules.Add("un cierre con \"heading\" de nivel 2 (\"Conclusión\") y párrafo");
        if (request.IncludeFaq) rules.Add("una sección \"Preguntas frecuentes\" (heading 2 + pares heading 3 / paragraph)");
        if (request.IncludeCta) rules.Add("un \"callout\" de llamada a la acción");
        if (request.IncludeLinks) rules.Add("enlaces internos con <a href=\"/articulos/{slug}\"> y externos autorizados");

        var links = request.IncludeLinks && request.InternalLinks.Count > 0
            ? "Enlaces internos disponibles:\n" + string.Join("\n", request.InternalLinks.Select(l => $"- {l.Title} → /articulos/{l.Slug}")) + "\n"
            : string.Empty;

        return
            $"Reescribe el siguiente artículo en {language} para convertirlo en una versión 100% original, más completa, actualizada y útil, manteniendo el tema pero sin copiar frases ni párrafos.\n" +
            topic +
            $"Idioma de salida obligatorio: {language}. Todo el contenido visible (título, resumen, meta descripción, bloques y etiquetas) debe ir íntegramente en {language}.\n" +
            $"Tono editorial: {OpenAiCompatibleArticleGenerator.DescribeTone(request.Tone)}.\n" +
            $"Estructura: {OpenAiCompatibleArticleGenerator.DescribeTemplate(request.Template)}.\n" +
            length +
            (rules.Count > 0 ? $"Secciones requeridas: {string.Join("; ", rules)}.\n" : string.Empty) +
            OpenAiCompatibleArticleGenerator.DescribeForbiddenSections(
                request.IncludeConclusion, request.IncludeFaq, request.IncludeCta, request.Template) +
            links +
            "Artículo original:\n" +
            $"Título: {request.Title}\nResumen: {request.Summary}\n\n{source}\n" +
            "Devuelve SOLO un JSON válido (sin markdown) con esta forma:\n" +
            "{\n  \"title\": \"...\",\n  \"summary\": \"...\",\n  \"metaDescription\": \"...\",\n  \"categorySuggestion\": \"...\",\n  \"tags\": [\"...\"],\n  \"blocks\": [ { \"type\": \"heading\", \"level\": 2, \"text\": \"...\" }, { \"type\": \"paragraph\", \"text\": \"...\" }, { \"type\": \"bullet-list\", \"items\": [\"...\"] } ]\n}\n" +
            "Reglas: primer bloque \"paragraph\"; sin encabezados de nivel 1; sin dos encabezados seguidos; tipos permitidos heading (2/3), paragraph, bullet-list, ordered-list, quote, callout, table, divider y code; sin imágenes. " +
            "El contenido debe ser 100% original y atribuir los datos a sus fuentes.";
    }

    private static string RenderBlocks(IEnumerable<ContentBlockDto> blocks)
    {
        var sb = new StringBuilder();
        foreach (var block in blocks)
        {
            switch (block)
            {
                case ContentHeadingBlockDto h:
                    sb.AppendLine().AppendLine($"{(h.Level == 3 ? "###" : "##")} {h.Text}");
                    break;
                case ContentParagraphBlockDto p:
                    sb.AppendLine(p.Text);
                    break;
                case ContentBulletListBlockDto l:
                    foreach (var item in l.Items) sb.AppendLine($"- {item}");
                    break;
                case ContentOrderedListBlockDto l:
                    foreach (var item in l.Items) sb.AppendLine($"1. {item}");
                    break;
                case ContentQuoteBlockDto q:
                    sb.AppendLine($"> {q.Text}");
                    break;
                case ContentCalloutBlockDto c:
                    sb.AppendLine($"NOTA: {c.Text}");
                    break;
                case ContentTableBlockDto t:
                    sb.AppendLine(string.Join(" | ", t.Headers));
                    foreach (var row in t.Rows) sb.AppendLine(string.Join(" | ", row));
                    break;
                case ContentCodeBlockDto c:
                    sb.AppendLine(c.Code);
                    break;
            }
        }
        return sb.ToString();
    }

    private const string SystemPrompt =
        "Eres un editor SEO senior. Redactas siempre en el idioma que se te indique, con contenido 100% original, y respondes únicamente con JSON válido y sin comentarios.";
}
