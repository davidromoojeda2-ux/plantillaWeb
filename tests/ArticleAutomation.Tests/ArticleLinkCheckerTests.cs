using System.Net;
using Infrastructure.Links;

namespace ArticleAutomation.Tests;

public class ArticleLinkCheckerTests
{
    [Fact]
    public async Task Marca_los_enlaces_404_como_caidos()
    {
        var handler = new StubHttpMessageHandler(request =>
            request.RequestUri!.AbsolutePath == "/roto"
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK));

        var checker = new ArticleLinkChecker(new HttpClient(handler));

        var results = await checker.CheckAsync(
            ["https://example.com/roto", "https://example.com/ok"],
            new Uri("https://example.com/"));

        Assert.True(results.Single(r => r.Url.Contains("/roto")).IsBroken);
        Assert.False(results.Single(r => r.Url.Contains("/ok")).IsBroken);
    }

    [Fact]
    public async Task Reintenta_con_GET_cuando_HEAD_no_esta_permitido()
    {
        var handler = new StubHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
                : new HttpResponseMessage(HttpStatusCode.OK));

        var checker = new ArticleLinkChecker(new HttpClient(handler));

        var results = await checker.CheckAsync(
            ["https://example.com/recurso"],
            new Uri("https://example.com/"));

        var result = Assert.Single(results);
        Assert.False(result.IsBroken);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task Resuelve_enlaces_relativos_contra_la_url_base()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var checker = new ArticleLinkChecker(new HttpClient(handler));

        var results = await checker.CheckAsync(
            ["/articulos/otro"],
            new Uri("https://midominio.com/"));

        var result = Assert.Single(results);
        Assert.Equal("https://midominio.com/articulos/otro", result.Url);
    }

    [Fact]
    public async Task No_comprueba_hosts_privados_ajenos_al_sitio()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var checker = new ArticleLinkChecker(new HttpClient(handler));

        var results = await checker.CheckAsync(
            ["http://192.168.1.10/panel", "http://localhost:9999/otro"],
            new Uri("https://midominio.com/"));

        Assert.Empty(results);
    }
}
