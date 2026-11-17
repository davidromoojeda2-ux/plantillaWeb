using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArticleAutomation.Tests;

public class OpenAiCompatibleArticleRewriterTests
{
    [Fact]
    public async Task RewriteAsync_Does_Not_Request_Optional_Sections_When_Not_Selected()
    {
        var prompt = await CapturePromptAsync(
            "job", includeConclusion: false, includeFaq: false, includeCta: false);

        Assert.Contains("No agregues una sección de \"Conclusión\"", prompt);
        Assert.DoesNotContain("un cierre con \"heading\" de nivel 2 (\"Conclusión\")", prompt);
        Assert.DoesNotContain("una sección \"Preguntas frecuentes\" (heading 2", prompt);
        Assert.DoesNotContain("un \"callout\" de llamada a la acción", prompt);
    }

    [Fact]
    public async Task RewriteAsync_Requests_Optional_Sections_Only_When_Selected()
    {
        var prompt = await CapturePromptAsync(
            "job", includeConclusion: true, includeFaq: true, includeCta: true);

        Assert.DoesNotContain("No agregues una sección", prompt);
        Assert.DoesNotContain("No agregues una llamada", prompt);
        Assert.Contains("un cierre con \"heading\" de nivel 2 (\"Conclusión\") y párrafo", prompt);
        Assert.Contains("una sección \"Preguntas frecuentes\" (heading 2", prompt);
        Assert.Contains("un \"callout\" de llamada a la acción", prompt);
    }

    [Fact]
    public async Task RewriteAsync_Uses_Selected_Tone()
    {
        var prompt = await CapturePromptAsync("news", "cronica");

        Assert.Contains("Tono narrativo de crónica", prompt);
    }

    private static Task<string> CapturePromptAsync(
        string template, string tone = "informativo",
        bool includeConclusion = false, bool includeFaq = false, bool includeCta = false)
        => OpenAiPromptCapture.CaptureAsync(async http =>
        {
            var rewriter = new OpenAiCompatibleArticleRewriter(
                http, NullLogger<OpenAiCompatibleArticleRewriter>.Instance);

            await rewriter.RewriteAsync(new ArticleRewriteRequest
            {
                Title = "Artículo original",
                Summary = "Resumen original",
                Blocks = [new ContentParagraphBlockDto { Text = "Contenido original." }],
                ApiKey = "k",
                Template = template,
                Tone = tone,
                IncludeConclusion = includeConclusion,
                IncludeFaq = includeFaq,
                IncludeCta = includeCta
            });
        });
}
