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

        var topic = Assert.Single(result.Topics);
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

        Assert.Empty(result.Topics);
    }

    [Fact]
    public async Task DiscoverAsync_RoundRobin_Interleaves_Results()
    {
        var call = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            call++;
            var json = call == 1
                ? """{"news":[{"title":"A1"},{"title":"A2"}],"credits":1}"""
                : """{"news":[{"title":"B1"},{"title":"B2"}],"credits":1}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var service = new SerperTopicDiscoveryService(
            new HttpClient(handler),
            NullLogger<SerperTopicDiscoveryService>.Instance);

        var result = await service.DiscoverAsync(
            new TopicDiscoveryRequest { Topics = ["a", "b"], Limit = 4, RoundRobin = true, ApiKey = "k" },
            CancellationToken.None);

        Assert.Equal(["A1", "B1", "A2", "B2"], result.Topics.Select(t => t.Title).ToArray());
        Assert.Equal(2, result.Credits);
    }

    [Fact]
    public async Task DiscoverAsync_Sequential_Keeps_Query_Order()
    {
        var call = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            call++;
            var json = call == 1
                ? """{"news":[{"title":"A1"},{"title":"A2"}],"credits":1}"""
                : """{"news":[{"title":"B1"},{"title":"B2"}],"credits":1}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var service = new SerperTopicDiscoveryService(
            new HttpClient(handler),
            NullLogger<SerperTopicDiscoveryService>.Instance);

        var result = await service.DiscoverAsync(
            new TopicDiscoveryRequest { Topics = ["a", "b"], Limit = 4, RoundRobin = false, ApiKey = "k" },
            CancellationToken.None);

        Assert.Equal(["A1", "A2", "B1", "B2"], result.Topics.Select(t => t.Title).ToArray());
    }
}
