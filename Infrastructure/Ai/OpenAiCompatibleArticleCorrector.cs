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
        var source = OpenAiCompatibleArticleRewriter.RenderBlocks(
            request.Blocks, includeBlockNumbers: true);
        var profile = ParagraphStyleProfile.Resolve(
            request.Template, request.ParagraphStyles, request.Language);

        var issues = request.Issues.Count > 0
            ? "Observaciones concretas (corrige todas; cada una indica el bloque afectado):\n" +
              string.Join("\n", request.Issues.Select(
                  (issue, index) => $"{index + 1}. {issue}")) + "\n"
            : "No hay observaciones: devuelve el artículo tal cual, sin cambios de sentido.\n";

        var links = request.InternalLinks.Count > 0
            ? "Enlaces internos disponibles para proponer reemplazos:\n" +
              string.Join("\n", request.InternalLinks.Select(
                  link => $"- {link.Title} → /articulos/{link.Slug}")) + "\n"
            : string.Empty;

        var rules =
            $"- Cada \"paragraph\" debe tener entre {profile.MinWords} y {profile.MaxWords} palabras. " +
            $"Si supera {profile.MaxWords}, divídelo en dos o más párrafos; si no llega a {profile.MinWords}, amplíalo con información concreta.\n" +
            $"- No encadenes más de {profile.MaxConsecutive} párrafos seguidos sin un \"heading\", lista, tabla o cita.\n" +
            $"- La primera frase de cada sección debe responder al encabezado en {profile.MaxFirstSentenceWords} palabras o menos y sin fórmulas de relleno.\n";

        return
            $"Reescribe el artículo completo en {language} para corregir TODAS las observaciones. " +
            "No cambies el tema ni inventes información; conserva los datos y las fuentes.\n" +
            "La corrección se valida automáticamente: si un párrafo queda fuera del rango de palabras o se incumple una regla, se considerará fallida.\n" +
            "Reglas obligatorias:\n" + rules +
            issues +
            ParagraphStyle.DescribeForPrompt(profile) + "\n" +
            links +
            "No modifiques los href de las etiquetas <a> dentro de los bloques: los cambios de enlaces se proponen aparte en \"linkActions\" y el usuario los confirmará.\n" +
            "Para cada enlace caído propone UNA acción: \"replace\" con un enlace interno disponible o una URL válida que conozcas, o \"remove\" si no hay un reemplazo fiable. Indica el motivo y no inventes URLs.\n" +
            "Puedes dividir, fusionar o añadir párrafos y añadir encabezados, listas o citas para cumplir las reglas; conserva el orden lógico y la información relevante.\n" +
            "En el texto cada bloque aparece precedido por su número (por ejemplo, [Bloque 7]); usa esa referencia para localizar cada observación.\n" +
            "No copies las marcas [Bloque N] en los textos del JSON de salida: son solo referencias internas.\n" +
            "Artículo actual:\n" +
            $"Título: {request.Title}\nResumen: {request.Summary}\n\n{source}\n" +
            OpenAiCompatibleArticleGenerator.JsonOutputInstruction +
            OpenAiCompatibleArticleGenerator.ArticleJsonShapeWithLinkActions +
            "Antes de responder, cuenta las palabras de cada \"paragraph\" y comprueba que está entre " +
            $"{profile.MinWords} y {profile.MaxWords}; divide los largos y amplía los cortos. " +
            "Reglas: primer bloque \"paragraph\"; sin encabezados de nivel 1; sin dos encabezados seguidos; tipos permitidos heading (2/3), paragraph, bullet-list, ordered-list, quote, callout, table y code. " +
            "No incluyas bloques image ni document: las imágenes y documentos del artículo original se conservan automáticamente. " +
            "Mantén el contenido 100% original y atribuye los datos a sus fuentes.";
    }

    private const string SystemPrompt =
        "Eres un editor SEO senior. Reescribes artículos existentes para corregir sus problemas editoriales, mantienes su voz y respondes únicamente con JSON válido y sin comentarios.";
}
