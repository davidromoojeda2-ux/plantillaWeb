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
            choices = new[] { new { message = new { content } } },
            usage = new { prompt_tokens = 120, completion_tokens = 30, total_tokens = 150 }
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
        Assert.NotNull(result.TotalTokens);
        Assert.Equal(150, result.TotalTokens);
    }

    [Fact]
    public async Task GenerateAsync_Does_Not_Request_Optional_Sections_When_Not_Selected()
    {
        var templates = new[]
        {
            "news", "informative", "comparative", "analysis", "guide", "faq", "ppa", "job"
        };

        foreach (var template in templates)
        {
            var prompt = await CapturePromptAsync(template);

            Assert.DoesNotContain("cierra con un \"heading\" de nivel 2 (\"Conclusión\")", prompt);
            Assert.DoesNotContain("incluye una sección \"Preguntas frecuentes\"", prompt);
            Assert.DoesNotContain("con una llamada a la acción", prompt);
            Assert.Contains("No agregues una sección de \"Conclusión\"", prompt);
        }
    }

    [Fact]
    public async Task GenerateAsync_Requests_Optional_Sections_Only_When_Selected()
    {
        var prompt = await CapturePromptAsync(
            "news", includeConclusion: true, includeFaq: true, includeCta: true);

        Assert.Contains("cierra con un \"heading\" de nivel 2 (\"Conclusión\")", prompt);
        Assert.Contains("incluye una sección \"Preguntas frecuentes\"", prompt);
        Assert.Contains("con una llamada a la acción", prompt);
        Assert.DoesNotContain("No agregues una sección", prompt);
        Assert.DoesNotContain("No agregues una llamada", prompt);
    }

    [Fact]
    public async Task GenerateAsync_Forbids_Only_The_Unselected_Optional_Sections()
    {
        var prompt = await CapturePromptAsync(
            "news", includeConclusion: true, includeFaq: false, includeCta: false);

        Assert.Contains("cierra con un \"heading\" de nivel 2 (\"Conclusión\")", prompt);
        Assert.Contains("No agregues una sección de \"Preguntas frecuentes\"", prompt);
        Assert.DoesNotContain("No agregues una sección de \"Conclusión\"", prompt);
    }

    [Fact]
    public async Task GenerateAsync_Does_Not_Forbid_Faq_When_The_Template_Is_Faq()
    {
        var prompt = await CapturePromptAsync("faq");

        Assert.Contains("Artículo de preguntas frecuentes", prompt);
        Assert.DoesNotContain("No agregues una sección de \"Preguntas frecuentes\"", prompt);
        Assert.Contains("No agregues una sección de \"Conclusión\"", prompt);
    }

    [Fact]
    public async Task GenerateAsync_Produces_A_Distinct_Prompt_Per_Template()
    {
        var markers = new Dictionary<string, string>
        {
            ["news"] = "Noticia periodística",
            ["informative"] = "Artículo informativo con enfoque explicativo",
            ["comparative"] = "Artículo comparativo entre opciones",
            ["analysis"] = "Artículo de análisis e interpretación",
            ["guide"] = "Guía práctica orientada a la acción",
            ["faq"] = "Artículo de preguntas frecuentes",
            ["ppa"] = "Respuesta tipo \"People Also Ask\"",
            ["job"] = "Convocatoria laboral"
        };

        var prompts = new List<string>();

        foreach (var (template, marker) in markers)
        {
            var prompt = await CapturePromptAsync(template);
            Assert.Contains(marker, prompt);
            prompts.Add(prompt);
        }

        Assert.Equal(markers.Count, prompts.Distinct(StringComparer.Ordinal).Count());
    }

    private static async Task<string> CapturePromptAsync(
        string template, bool includeConclusion = false, bool includeFaq = false, bool includeCta = false)
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

        var service = new OpenAiCompatibleArticleGenerator(
            new HttpClient(handler),
            NullLogger<OpenAiCompatibleArticleGenerator>.Instance);

        await service.GenerateAsync(new TopicGenerationRequest
        {
            TopicTitle = "Tema de prueba",
            ApiKey = "k",
            Template = template,
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
