using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArticleAutomation.Tests;

public class OpenAiCompatibleArticleCorrectorTests
{
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
        Assert.Contains("No cambies el tema", prompt);
        Assert.Contains("entre 40 y 70", prompt);
        Assert.Contains("No inventes URLs", prompt);
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
