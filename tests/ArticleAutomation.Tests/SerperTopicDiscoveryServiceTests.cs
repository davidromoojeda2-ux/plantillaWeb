using System.Net;
using System.Text;
using Aplication.Interfaces;
using Infrastructure.Serper;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArticleAutomation.Tests;

public class SerperTopicDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoverAsync_Parses_News_Topics()
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var json = """{"news":[{"title":"Tema Uno","source":"Fuente","snippet":"Resumen","link":"https://ejemplo.com"}]}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var service = new SerperTopicDiscoveryService(
            new HttpClient(handler),
            NullLogger<SerperTopicDiscoveryService>.Instance);

        var result = await service.DiscoverAsync(
            new TopicDiscoveryRequest { Topics = ["deportes"], Country = "mx", Language = "es", ApiKey = "k" },
            CancellationToken.None);

        var topic = Assert.Single(result);
        Assert.Equal("Tema Uno", topic.Title);
        Assert.Equal("Fuente", topic.Source);
    }

    [Fact]
    public async Task DiscoverAsync_Without_ApiKey_Returns_Empty()
    {
        var service = new SerperTopicDiscoveryService(
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            NullLogger<SerperTopicDiscoveryService>.Instance);

        var result = await service.DiscoverAsync(
            new TopicDiscoveryRequest { Topics = ["deportes"] },
            CancellationToken.None);

        Assert.Empty(result);
    }
}
