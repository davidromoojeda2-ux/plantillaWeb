using System.Net;
using System.Text;
using System.Text.Json;
using Aplication.Interfaces;
using Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArticleAutomation.Tests;

public class OpenAiCompatibleTopicCountryValidatorTests
{
    [Fact]
    public async Task ValidateAsync_Maps_Response_To_Result()
    {
        var content = JsonSerializer.Serialize(new
        {
            relevante = false,
            motivo = "El tema pertenece a otro país."
        });

        var chat = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content } } },
            usage = new { prompt_tokens = 25, completion_tokens = 8, total_tokens = 33 }
        });

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(chat, Encoding.UTF8, "application/json")
        });

        var validator = new OpenAiCompatibleTopicCountryValidator(
            new HttpClient(handler),
            NullLogger<OpenAiCompatibleTopicCountryValidator>.Instance);

        var result = await validator.ValidateAsync(new TopicCountryCheckRequest
        {
            TopicTitle = "Elecciones en otro país",
            CountryCode = "mx",
            Language = "es",
            ApiKey = "k",
            BaseUrl = "https://example.com",
            Model = "m"
        });

        Assert.False(result.IsRelevant);
        Assert.Equal("El tema pertenece a otro país.", result.Reason);
        Assert.Equal(33, result.TotalTokens);
    }

    [Fact]
    public async Task ValidateAsync_Accepts_Textual_True()
    {
        var content = JsonSerializer.Serialize(new
        {
            relevante = "sí",
            motivo = "Tema global sin ubicación clara."
        });

        var chat = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content } } },
            usage = new { total_tokens = 10 }
        });

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(chat, Encoding.UTF8, "application/json")
        });

        var validator = new OpenAiCompatibleTopicCountryValidator(
            new HttpClient(handler),
            NullLogger<OpenAiCompatibleTopicCountryValidator>.Instance);

        var result = await validator.ValidateAsync(new TopicCountryCheckRequest
        {
            TopicTitle = "Tema global",
            CountryCode = "es",
            ApiKey = "k"
        });

        Assert.True(result.IsRelevant);
        Assert.Equal(10, result.TotalTokens);
    }

    [Fact]
    public async Task ValidateAsync_Throws_When_ApiKey_Is_Missing()
    {
        var validator = new OpenAiCompatibleTopicCountryValidator(
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            NullLogger<OpenAiCompatibleTopicCountryValidator>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.ValidateAsync(new TopicCountryCheckRequest { TopicTitle = "Tema", ApiKey = "" }));
    }
}
