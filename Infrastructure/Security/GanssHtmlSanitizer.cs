using System.Net;
using System.Text.RegularExpressions;
using Aplication.Interfaces;
using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace Infrastructure.Security;

/// <summary>
/// Sanitizador de HTML inline con una allowlist cerrada. Solo permite formato
/// básico y enlaces HTTP/HTTPS/mailto; elimina estilos, clases, ids, eventos y
/// esquemas peligrosos.
/// </summary>
public sealed class GanssHtmlSanitizer : Aplication.Interfaces.IHtmlSanitizer
{
    private static readonly string[] AllowedTags =
        ["strong", "b", "em", "i", "u", "s", "a", "code", "br"];

    private readonly Ganss.Xss.HtmlSanitizer _sanitizer;
    private readonly HtmlParser _parser = new();

    public GanssHtmlSanitizer()
    {
        _sanitizer = new Ganss.Xss.HtmlSanitizer();

        _sanitizer.AllowedTags.Clear();
        foreach (var tag in AllowedTags)
            _sanitizer.AllowedTags.Add(tag);

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("title");

        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");

        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedAtRules.Clear();
        _sanitizer.KeepChildNodes = true;
    }

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        // Ganss conserva los nodos hijos de etiquetas prohibidas. Para scripts,
        // estilos y contenido activo eso filtraría su texto al render; se eliminan
        // antes de aplicar la allowlist.
        var input = _parser.ParseDocument($"<body>{html}</body>");
        foreach (var dangerous in input.QuerySelectorAll(
                     "script,style,iframe,object,embed,svg,math,template,noscript"))
        {
            dangerous.Remove();
        }

        var clean = _sanitizer.Sanitize(input.Body?.InnerHtml ?? string.Empty);
        if (string.IsNullOrWhiteSpace(clean)) return string.Empty;

        var document = _parser.ParseDocument($"<body>{clean}</body>");
        var body = document.Body;
        if (body is null) return string.Empty;

        foreach (var anchor in body.QuerySelectorAll("a"))
        {
            var href = anchor.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href)) continue;

            if (Uri.TryCreate(href, UriKind.Absolute, out var uri) &&
                uri.Scheme is "http" or "https")
            {
                anchor.SetAttribute("target", "_blank");
                anchor.SetAttribute("rel", "noopener noreferrer");
            }
            else
            {
                anchor.RemoveAttribute("target");
                anchor.RemoveAttribute("rel");
            }
        }

        return body.InnerHtml;
    }

    public string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        // Los saltos de línea y cierres de bloque no generan espacios en el
        // TextContent de AngleSharp; se insertan explícitamente.
        var separated = Regex.Replace(
            html,
            @"<\s*/?\s*(br|p|div|li|h[1-6]|blockquote|tr|td|th|table|ul|ol|pre)\b[^>]*>",
            " ",
            RegexOptions.IgnoreCase);

        var document = _parser.ParseDocument($"<body>{separated}</body>");
        var text = document.Body?.TextContent ?? string.Empty;
        text = WebUtility.HtmlDecode(text);
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
