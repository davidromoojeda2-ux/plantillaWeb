using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Aplication.Services;
using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace Infrastructure.Ai;

/// <summary>
/// Corrige un artículo existente mediante un endpoint compatible con OpenAI. El
/// prompt parte de las observaciones de validación y del mismo perfil de párrafo que
/// usa el editor, de modo que la corrección apunte exactamente a lo señalado.
/// </summary>
public sealed class OpenAiCompatibleArticleCorrector : IAiArticleCorrector
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenAiCompatibleArticleCorrector> _logger;
    private readonly ResiliencePipeline _pipeline;

    public OpenAiCompatibleArticleCorrector(
        HttpClient http, ILogger<OpenAiCompatibleArticleCorrector> logger)
    {
        _http = http;
        _logger = logger;
        _pipeline = ResiliencePolicies.CreateHttpPipeline(TimeSpan.FromSeconds(180));
    }

    public async Task<GeneratedArticle> CorrectAsync(
        ArticleCorrectionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new InvalidOperationException("La IA no está configurada (clave ausente).");

        var baseUrl = string.IsNullOrWhiteSpace(request.BaseUrl)
            ? "https://api.openai.com/v1"
            : request.BaseUrl.TrimEnd('/');
        var model = string.IsNullOrWhiteSpace(request.Model) ? "gpt-4o-mini" : request.Model;
        var prompt = BuildPrompt(request);

        var content = await OpenAiCompatibleArticleGenerator.SendChatAsync(
            _http, _pipeline, baseUrl, request.ApiKey, model,
            request.Temperature, request.MaxTokens, SystemPrompt, prompt, ct);

        _logger.LogDebug(
            "Artículo corregido a partir de {Issues} observaciones.", request.Issues.Count);

        var article = OpenAiCompatibleArticleGenerator.MapResponse(content);
        article.Blocks = MergePreservedBlocks(request.Blocks, article.Blocks);
        return article;
    }

    /// <summary>
    /// Los bloques de imagen, documento y separador no forman parte del texto que ve
    /// el modelo, así que se reinsertan en su posición relativa para no perderlos ni
    /// permitir que el modelo invente archivos.
    /// </summary>
    public static List<ContentBlockDto> MergePreservedBlocks(
        IReadOnlyList<ContentBlockDto> original, IReadOnlyList<ContentBlockDto> corrected)
    {
        var textBlocks = corrected.Where(block => !IsPreserved(block)).ToList();
        var preserved = original
            .Select((block, index) => (block, index))
            .Where(item => IsPreserved(item.block))
            .ToList();

        // De atrás hacia delante para que cada inserción no desplace los índices
        // calculados para los bloques anteriores.
        for (var i = preserved.Count - 1; i >= 0; i--)
        {
            var (block, index) = preserved[i];
            var textBefore = original.Take(index).Count(candidate => !IsPreserved(candidate));
            textBlocks.Insert(Math.Min(textBefore, textBlocks.Count), block);
        }

        return textBlocks;
    }

    private static bool IsPreserved(ContentBlockDto block) =>
        block is ContentImageBlockDto or ContentDocumentBlockDto or ContentDividerBlockDto;

    internal static string BuildPrompt(ArticleCorrectionRequest request)
    {
        var language = OpenAiCompatibleArticleGenerator.DescribeLanguage(request.Language);
        var source = OpenAiCompatibleArticleRewriter.RenderBlocks(request.Blocks);
        var profile = ParagraphStyleProfile.Resolve(
            request.Template, request.ParagraphStyles, request.Language);

        var issues = request.Issues.Count > 0
            ? "Observaciones que debes corregir (y solo estas):\n" +
              string.Join("\n", request.Issues.Select(
                  (issue, index) => $"{index + 1}. {issue}")) + "\n"
            : "No hay observaciones: devuelve el artículo tal cual, sin cambios de sentido.\n";

        var links = request.InternalLinks.Count > 0
            ? "Enlaces internos disponibles para sustituir enlaces caídos:\n" +
              string.Join("\n", request.InternalLinks.Select(
                  link => $"- {link.Title} → /articulos/{link.Slug}")) + "\n"
            : string.Empty;

        return
            $"Corrige el siguiente artículo en {language} aplicando únicamente las observaciones indicadas.\n" +
            $"Idioma de salida obligatorio: {language}. No cambies el tema ni reescribas lo que ya es correcto.\n" +
            issues +
            ParagraphStyle.DescribeForPrompt(profile) + "\n" +
            links +
            "Si una observación indica un enlace caído, elimina el enlace dejando el texto visible o sustitúyelo por uno de los enlaces internos disponibles. No inventes URLs.\n" +
            "Conserva la estructura y el orden de los bloques; solo divide, amplía o reescribe los párrafos necesarios.\n" +
            "Artículo actual:\n" +
            $"Título: {request.Title}\nResumen: {request.Summary}\n\n{source}\n" +
            "Devuelve SOLO un JSON válido (sin markdown) con esta forma:\n" +
            "{\n  \"title\": \"...\",\n  \"summary\": \"...\",\n  \"metaDescription\": \"...\",\n  \"categorySuggestion\": \"...\",\n  \"tags\": [\"...\"],\n  \"blocks\": [ { \"type\": \"heading\", \"level\": 2, \"text\": \"...\" }, { \"type\": \"paragraph\", \"text\": \"...\" }, { \"type\": \"bullet-list\", \"items\": [\"...\"] } ]\n}\n" +
            "Reglas: primer bloque \"paragraph\"; sin encabezados de nivel 1; sin dos encabezados seguidos; tipos permitidos heading (2/3), paragraph, bullet-list, ordered-list, quote, callout, table y code. " +
            "No incluyas bloques image ni document: las imágenes y documentos del artículo original se conservan automáticamente. " +
            "Mantén el contenido 100% original y atribuye los datos a sus fuentes.";
    }

    private const string SystemPrompt =
        "Eres un editor SEO senior. Corriges artículos existentes con precisión quirúrgica, mantienes su voz y respondes únicamente con JSON válido y sin comentarios.";
}
