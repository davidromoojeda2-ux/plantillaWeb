using Domine.Content;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.ValueObjects;

/// <summary>
/// Texto enriquecido con dos representaciones: el HTML inline ya sanitizado que se
/// puede renderizar de forma segura y el texto plano derivado que se usa para
/// búsqueda, tiempo de lectura y metadatos.
/// </summary>
public sealed class RichText
{
    [BsonElement("html")]
    public string Html { get; private set; } = string.Empty;

    [BsonElement("text")]
    public string Text { get; private set; } = string.Empty;

    private RichText()
    {
    }

    private RichText(string html, string text)
    {
        Html = html;
        Text = text;
    }

    public static RichText Create(
        string? html,
        string? text,
        int maxHtmlLength = ContentLimits.MaxRichTextHtmlLength,
        int maxTextLength = ContentLimits.MaxTextLength)
    {
        var safeHtml = (html ?? string.Empty).Trim();
        var plain = (text ?? string.Empty).Trim();

        if (safeHtml.Length > maxHtmlLength)
            throw new ArgumentOutOfRangeException(nameof(html), $"El HTML del bloque supera el máximo de {maxHtmlLength} caracteres.");

        if (plain.Length > maxTextLength)
            throw new ArgumentOutOfRangeException(nameof(text), $"El texto del bloque supera el máximo de {maxTextLength} caracteres.");

        return new RichText(safeHtml, plain);
    }

    public static RichText Empty() => new(string.Empty, string.Empty);
}
