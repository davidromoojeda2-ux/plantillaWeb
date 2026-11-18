namespace paginaWeb.Seo;

/// <summary>
/// Construye la URL canónica de una página: ruta absoluta sin parámetros de
/// seguimiento, conservando solo los que cambian realmente el contenido
/// (por defecto, <c>page</c> de la paginación).
/// </summary>
public static class CanonicalUrl
{
    private const string PageQueryKey = "page";

    private static readonly string[] DefaultAllowedQueryKeys = [PageQueryKey];

    public static string Build(
        string baseUrl,
        string path,
        IEnumerable<KeyValuePair<string, string?>>? query = null,
        IEnumerable<string>? allowedQueryKeys = null)
    {
        var allowed = new HashSet<string>(
            allowedQueryKeys ?? DefaultAllowedQueryKeys,
            StringComparer.OrdinalIgnoreCase);

        var pairs = (query ?? [])
            .Where(pair => allowed.Contains(pair.Key))
            .Select(pair => (pair.Key, Value: pair.Value?.Trim()))
            .Where(pair => !string.IsNullOrEmpty(pair.Value))
            // page=1 es la URL sin parámetro: evita duplicar la primera página.
            .Where(pair => !(IsPageKey(pair.Key) && pair.Value == "1"))
            .ToList();

        var url = $"{baseUrl.TrimEnd('/')}{NormalizePath(path)}";

        if (pairs.Count == 0)
            return url;

        var queryString = string.Join(
            "&",
            pairs.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));

        return $"{url}?{queryString}";
    }

    private static bool IsPageKey(string key)
        => string.Equals(key, PageQueryKey, StringComparison.OrdinalIgnoreCase);

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/")
            return "/";

        var trimmed = path.Trim();
        return trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";
    }
}
