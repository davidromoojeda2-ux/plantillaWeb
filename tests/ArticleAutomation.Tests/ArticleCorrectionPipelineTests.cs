using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Aplication.Services;

namespace ArticleAutomation.Tests;

public class ArticleCorrectionPipelineTests
{
    [Fact]
    public async Task Reintenta_Cuando_Quedan_Parrafos_Fuera_Del_Rango()
    {
        var source = Article("Párrafo inicial correcto para el artículo de prueba.");
        var shortParagraph = Article(Words(20));
        var corrected = Article(
            "Esta es la respuesta breve que abre el artículo. " + Words(50));

        var corrector = new StubCorrector(shortParagraph, corrected);

        var outcome = await ArticleCorrectionPipeline.CorrectAsync(
            source,
            ["Enlace caído: https://example.com/roto"],
            Plan(),
            Config(),
            corrector);

        Assert.Equal(2, corrector.Calls);
        Assert.Equal(2, outcome.Attempts);
        Assert.Empty(outcome.RemainingFindings);
        Assert.Contains(
            corrector.Requests[0].Issues,
            issue => issue.Contains("Enlace caído", StringComparison.Ordinal));
        Assert.Contains(
            corrector.Requests[1].Issues,
            issue => issue.Contains("Bloque 1", StringComparison.Ordinal)
                     && issue.Contains("mínimo", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Normaliza_Los_Parrafos_Largos_Antes_De_Validar()
    {
        var source = Article(
            "Esta es la respuesta breve que abre el artículo. " + Words(50));
        var longParagraph = Article(string.Join(' ', Enumerable.Repeat(
            "Esta es una frase de ejemplo con diez palabras exactas.", 18)));

        var corrector = new StubCorrector(longParagraph);

        var outcome = await ArticleCorrectionPipeline.CorrectAsync(
            source, [], Plan(), Config(), corrector);

        Assert.Equal(1, corrector.Calls);
        Assert.Empty(outcome.RemainingFindings);
        Assert.True(outcome.Article.Blocks.Count > 1);
    }

    [Fact]
    public async Task Falla_Si_La_Ia_No_Devuelve_Bloques()
    {
        var source = Article("Párrafo inicial correcto para el artículo de prueba.");
        var corrector = new StubCorrector(new GeneratedArticle());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ArticleCorrectionPipeline.CorrectAsync(
                source, [], Plan(), Config(), corrector));
    }

    private static GeneratedArticle Article(string text) => new()
    {
        Title = "Título de prueba",
        Summary = "Resumen suficientemente largo para validar el artículo.",
        MetaDescription = "Meta descripción del artículo.",
        Tags = ["uno", "dos", "tres"],
        Blocks = [new ContentParagraphBlockDto { Text = text }]
    };

    private static ArticleCorrectionRequest Config() => new()
    {
        ApiKey = "k",
        Template = "news"
    };

    private static ArticleGenerationPlan Plan() => new(
        TargetWords: 800,
        MaxTokens: 4000,
        MinBlocks: 3,
        WordTolerancePercent: 20,
        MaxGenerationAttempts: 3,
        Template: "news",
        Tone: "neutral",
        Temperature: 0.3,
        IncludeList: false,
        IncludeTable: false,
        IncludeQuote: false,
        IncludeConclusion: false,
        IncludeFaq: false,
        IncludeCta: false,
        IncludeLinks: false,
        ParagraphStyles: null,
        Language: "es");

    private static string Words(int count) =>
        string.Join(' ', Enumerable.Repeat("palabra", count));

    private sealed class StubCorrector : IAiArticleCorrector
    {
        private readonly Queue<GeneratedArticle> _results;

        public StubCorrector(params GeneratedArticle[] results) =>
            _results = new Queue<GeneratedArticle>(results);

        public int Calls { get; private set; }

        public List<ArticleCorrectionRequest> Requests { get; } = [];

        public Task<GeneratedArticle> CorrectAsync(
            ArticleCorrectionRequest request, CancellationToken ct = default)
        {
            Calls++;
            Requests.Add(request);
            return Task.FromResult(_results.Dequeue());
        }
    }
}
