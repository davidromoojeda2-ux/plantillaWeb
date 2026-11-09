using System.Net;
using System.Text;
using System.Text.Json;
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

    private static async Task<string> CapturePromptAsync(
        string template, string tone = "informativo",
        bool includeConclusion = false, bool includeFaq = false, bool includeCta = false)
    {
        string? requestBody = null;

        var content = JsonSerializer.Serialize(new
        {
            title = "Título de prueba",
            summary = "Resumen de prueba",
            metaDescription = "Meta descripción",
            tags = new[] { "uno", "dos", "tres" },
            blocks = new object[] { new { type = "paragraph", text = "Contenido de prueba." } }
        });
        var chat = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content } } },
            usage = new { total_tokens = 1 }
        });

        var handler = new StubHttpMessageHandler(request =>
        {
            requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(chat, Encoding.UTF8, "application/json")
            };
        });

        var rewriter = new OpenAiCompatibleArticleRewriter(
            new HttpClient(handler),
            NullLogger<OpenAiCompatibleArticleRewriter>.Instance);

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

        using var document = JsonDocument.Parse(
            requestBody ?? throw new InvalidOperationException("No se capturó la petición."));

        return document.RootElement
            .GetProperty("messages")[1]
            .GetProperty("content")
            .GetString()!;
    }
}
