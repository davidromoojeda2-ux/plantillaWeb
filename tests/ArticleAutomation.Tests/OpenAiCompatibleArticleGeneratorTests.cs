using System.Net;
using System.Text;
using System.Text.Json;
using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArticleAutomation.Tests;

public class OpenAiCompatibleArticleGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Maps_Response_To_GeneratedArticle()
    {
        var article = new
        {
            title = "Título de prueba",
            summary = "Resumen del artículo generado",
            metaDescription = "Meta descripción",
            categorySuggestion = "Tecnología",
            tags = new[] { "ia", "tech" },
            blocks = new object[]
            {
                new { type = "heading", level = 2, text = "Introducción" },
                new { type = "paragraph", text = "Contenido del párrafo" },
                new { type = "bullet-list", items = new[] { "a", "b" } }
            }
        };

        var content = JsonSerializer.Serialize(article);
        var chat = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content } } }
        });

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(chat, Encoding.UTF8, "application/json")
        });

        var service = new OpenAiCompatibleArticleGenerator(
            new HttpClient(handler),
            NullLogger<OpenAiCompatibleArticleGenerator>.Instance);

        var result = await service.GenerateAsync(
            new TopicGenerationRequest
            {
                TopicTitle = "IA generativa",
                Language = "es",
                ApiKey = "k",
                BaseUrl = "https://example.com",
                Model = "m"
            },
            CancellationToken.None);

        Assert.Equal("Título de prueba", result.Title);
        Assert.Equal("Resumen del artículo generado", result.Summary);
        Assert.Equal(2, result.Tags.Count);
        Assert.Equal(3, result.Blocks.Count);
        Assert.IsType<ContentHeadingBlockDto>(result.Blocks[0]);
        Assert.IsType<ContentParagraphBlockDto>(result.Blocks[1]);
        Assert.IsType<ContentBulletListBlockDto>(result.Blocks[2]);
    }
}
