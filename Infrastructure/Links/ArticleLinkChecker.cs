using System.Net;
using System.Net.Http.Headers;
using Aplication.DTOs;
using Aplication.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Links;

/// <summary>
/// Comprueba enlaces con peticiones HEAD (y GET como respaldo) para detectar
/// recursos caídos, restringidos o que no responden.
/// </summary>
public sealed class ArticleLinkChecker : IArticleLinkChecker
{
    private const int MaxLinks = 40;
    private const int MaxConcurrency = 6;
    private const string UserAgent = "Mozilla/5.0 (compatible; PlantillaWebLinkChecker/1.0)";

    private readonly HttpClient _http;
    private readonly ILogger<ArticleLinkChecker>? _logger;

    public ArticleLinkChecker(HttpClient http, ILogger<ArticleLinkChecker>? logger = null)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ArticleLinkCheckResult>> CheckAsync(
        IEnumerable<string> links, Uri baseUri, CancellationToken ct = default)
    {
        var urls = links
            .Select(link => Normalize(link, baseUri))
            .Where(url => url is not null)
            .Select(url => url!)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxLinks)
            .ToList();

        if (urls.Count == 0) return [];

        using var gate = new SemaphoreSlim(MaxConcurrency);

        var tasks = urls.Select(async url =>
        {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                return await CheckOneAsync(url, ct).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        });

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task<ArticleLinkCheckResult> CheckOneAsync(string url, CancellationToken ct)
    {
        try
        {
            using var head = new HttpRequestMessage(HttpMethod.Head, url);
            head.Headers.UserAgent.ParseAdd(UserAgent);

            using var response = await _http.SendAsync(
                head, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            var code = (int)response.StatusCode;

            // Algunos servidores rechazan HEAD aunque el recurso exista.
            if (code is 400 or 403 or 405 or 429 or 501)
                return await CheckWithGetAsync(url, ct).ConfigureAwait(false);

            return Map(url, response.StatusCode);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ArticleLinkCheckResult(url, null, "sin-respuesta", "Tiempo de espera agotado");
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogDebug(ex, "HEAD falló para {Url}; se reintenta con GET.", url);
            return await CheckWithGetAsync(url, ct).ConfigureAwait(false);
        }
    }

    private async Task<ArticleLinkCheckResult> CheckWithGetAsync(string url, CancellationToken ct)
    {
        try
        {
            using var get = new HttpRequestMessage(HttpMethod.Get, url);
            get.Headers.UserAgent.ParseAdd(UserAgent);

            using var response = await _http.SendAsync(
                get, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            return Map(url, response.StatusCode);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ArticleLinkCheckResult(url, null, "sin-respuesta", "Tiempo de espera agotado");
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogDebug(ex, "No se pudo conectar con {Url}.", url);
            return new ArticleLinkCheckResult(url, null, "sin-respuesta", "No se pudo conectar");
        }
    }

    private static ArticleLinkCheckResult Map(string url, HttpStatusCode status)
    {
        var code = (int)status;

        if (code >= 200 && code < 300)
            return new ArticleLinkCheckResult(url, code, "ok", null);

        if (code >= 300 && code < 400)
            return new ArticleLinkCheckResult(url, code, "ok", "Redirección");

        if (code is 401 or 403 or 429)
            return new ArticleLinkCheckResult(url, code, "restringido", "Requiere autenticación o limita el acceso");

        if (code is 404 or 410)
            return new ArticleLinkCheckResult(url, code, "caido", "El recurso no existe");

        if (code >= 500)
            return new ArticleLinkCheckResult(url, code, "error-servidor", "Error del servidor");

        return new ArticleLinkCheckResult(url, code, "caido", $"HTTP {code}");
    }

    private static string? Normalize(string? link, Uri baseUri)
    {
        if (string.IsNullOrWhiteSpace(link)) return null;

        var raw = WebUtility.HtmlDecode(link.Trim());
        if (raw.StartsWith('#')) return null;

        if (raw.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("ftp:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (raw.StartsWith("//", StringComparison.Ordinal))
            raw = $"{baseUri.Scheme}:{raw}";
        else if (raw.StartsWith("~/", StringComparison.Ordinal))
            raw = raw[1..];

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) &&
            !Uri.TryCreate(baseUri, raw, out uri))
        {
            return null;
        }

        if (uri.Scheme is not ("http" or "https")) return null;

        // Evita comprobar hosts privados o loopback ajenos al propio sitio (SSRF).
        if (IsBlockedHost(uri, baseUri)) return null;

        var builder = new UriBuilder(uri) { Fragment = string.Empty };
        return builder.Uri.ToString();
    }

    private static bool IsBlockedHost(Uri uri, Uri baseUri)
    {
        if (string.Equals(uri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase) &&
            uri.Port == baseUri.Port)
        {
            return false;
        }

        if (uri.IsLoopback) return true;

        if (!IPAddress.TryParse(uri.Host, out var address))
            return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);

        var bytes = address.GetAddressBytes();

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254);
        }

        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;
    }
}
