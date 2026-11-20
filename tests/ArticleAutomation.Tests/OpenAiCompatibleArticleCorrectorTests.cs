using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ArticleAutomation.Tests;

public class OpenAiCompatibleArticleCorrectorTests
{
    [Fact]
    public async Task CorrectAsync_Maps_The_Link_Actions()
    {
        var content = JsonSerializer.Serialize(new
        {
            title = "Título corregido",
            summary = "Resumen corregido",
            metaDescription = "Meta descripción",
            tags = new[] { "uno", "dos", "tres" },
            blocks = new object[] { new { type = "paragraph", text = "Texto corregido." } },
            linkActions = new object[]
            {
                new { type = "replace", url = "https://roto", replacement = "/articulos/ok", reason = "Mejor enlace" },
                new { type = "remove", url = "https://roto2", reason = "No existe" }
            }
        });
        var chat = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content } } },
            usage = new { total_tokens = 1 }
        });

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(chat, Encoding.UTF8, "application/json")
        });
        var corrector = new OpenAiCompatibleArticleCorrector(
            new HttpClient(handler), NullLogger<OpenAiCompatibleArticleCorrector>.Instance);

        var article = await corrector.CorrectAsync(new ArticleCorrectionRequest
        {
            Title = "Original",
            Summary = "Resumen",
            Blocks = [new ContentParagraphBlockDto { Text = "Texto." }],
            ApiKey = "k"
        });

        Assert.Equal(2, article.LinkActions.Count);
        Assert.Equal("replace", article.LinkActions[0].Type);
        Assert.Equal("https://roto", article.LinkActions[0].Url);
        Assert.Equal("/articulos/ok", article.LinkActions[0].Replacement);
        Assert.Equal("remove", article.LinkActions[1].Type);
    }

    [Fact]
    public async Task CorrectAsync_Includes_The_Observations_And_The_Paragraph_Rules()
    {
        var prompt = await OpenAiPromptCapture.CaptureAsync(async http =>
        {
            var corrector = new OpenAiCompatibleArticleCorrector(
                http, NullLogger<OpenAiCompatibleArticleCorrector>.Instance);

            await corrector.CorrectAsync(new ArticleCorrectionRequest
            {
                Title = "Artículo original",
                Summary = "Resumen original",
                Blocks = [new ContentParagraphBlockDto { Text = "Texto original." }],
                Issues = ["El párrafo 1 tiene 120 palabras; divídelo."],
                ApiKey = "k",
                Template = "news"
            });
        });

        Assert.Contains("El párrafo 1 tiene 120 palabras; divídelo.", prompt);
        Assert.Contains("Reescribe el artículo completo", prompt);
        Assert.Contains("Reglas obligatorias", prompt);
        Assert.Contains("[Bloque 1]", prompt);
        Assert.Contains("se considerará fallida", prompt);
        Assert.Contains("No cambies el tema", prompt);
        Assert.Contains("entre 40 y 70", prompt);
        Assert.Contains("linkActions", prompt);
        Assert.Contains("No modifiques los href", prompt);
    }

    [Fact]
    public async Task CorrectAsync_Works_Without_Observations()
    {
        var prompt = await OpenAiPromptCapture.CaptureAsync(async http =>
        {
            var corrector = new OpenAiCompatibleArticleCorrector(
                http, NullLogger<OpenAiCompatibleArticleCorrector>.Instance);

            await corrector.CorrectAsync(new ArticleCorrectionRequest
            {
                Title = "Artículo original",
                Summary = "Resumen original",
                Blocks = [new ContentParagraphBlockDto { Text = "Texto original." }],
                ApiKey = "k"
            });
        });

        Assert.Contains("No hay observaciones", prompt);
    }

    [Fact]
    public void MergePreservedBlocks_Keeps_Media_In_Place()
    {
        var original = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto { Text = "a" },
            new ContentImageBlockDto { Url = "/img.png", Alt = "x" },
            new ContentParagraphBlockDto { Text = "b" },
            new ContentDividerBlockDto(),
            new ContentParagraphBlockDto { Text = "c" }
        };
        var corrected = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto { Text = "a2" },
            new ContentParagraphBlockDto { Text = "b2" },
            new ContentParagraphBlockDto { Text = "c2" },
            new ContentImageBlockDto { Url = "/inventada.png" }
        };

        var merged = OpenAiCompatibleArticleCorrector.MergePreservedBlocks(original, corrected);

        Assert.Equal(5, merged.Count);
        Assert.IsType<ContentParagraphBlockDto>(merged[0]);
        var image = Assert.IsType<ContentImageBlockDto>(merged[1]);
        Assert.Equal("/img.png", image.Url);
        Assert.IsType<ContentParagraphBlockDto>(merged[2]);
        Assert.IsType<ContentDividerBlockDto>(merged[3]);
        Assert.IsType<ContentParagraphBlockDto>(merged[4]);
    }
}
