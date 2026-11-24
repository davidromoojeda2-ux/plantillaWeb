using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace paginaWeb.Helpers;

/// <summary>
/// Helpers de presentación para los bloques editoriales: ids deterministas de
/// encabezados y clases de callout.
/// </summary>
public static class ContentBlockViewHelpers
{
    private static readonly Regex ListLeadRegex = new(
        @"^(?<indent>\s*)(?<lead>[^<>:\r\n]{1,80}):",
        RegexOptions.Compiled);

    public static string HeadingId(string? text, int index)
    {
        var slug = Slugify(text);
        return string.IsNullOrEmpty(slug) ? $"seccion-{index}" : $"seccion-{slug}-{index}";
    }

    /// <summary>
    /// Destaca el término inicial de un elemento de lista cuando tiene la forma
    /// «Etiqueta: descripción». Se aplica después de sanitizar el HTML, por lo que
    /// la etiqueta generada es segura y el contenido almacenado no se modifica.
    /// </summary>
    public static string HighlightListLead(string? sanitizedHtml)
    {
        if (string.IsNullOrWhiteSpace(sanitizedHtml)) return sanitizedHtml ?? string.Empty;

        var match = ListLeadRegex.Match(sanitizedHtml);
        if (!match.Success) return sanitizedHtml;

        var lead = match.Groups["lead"].Value;
        if (string.IsNullOrWhiteSpace(lead)) return sanitizedHtml;
        if (!lead.Any(char.IsLetter)) return sanitizedHtml;
        if (lead.Contains("http", StringComparison.OrdinalIgnoreCase) ||
            lead.Contains("www.", StringComparison.OrdinalIgnoreCase))
        {
            return sanitizedHtml;
        }

        var indent = match.Groups["indent"].Value;
        var rest = sanitizedHtml[(match.Index + match.Length)..];
        return $"{indent}<span class=\"list-item-lead\">{lead}:</span>{rest}";
    }

    private static string Slugify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var rune in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(rune) != UnicodeCategory.NonSpacingMark)
                builder.Append(rune);
        }

        var value = Regex.Replace(builder.ToString(), @"[^a-z0-9]+", "-");
        return value.Trim('-');
    }

    public static string CalloutClass(string? variant) => variant?.ToLowerInvariant() switch
    {
        "tip" => "callout callout-tip",
        "warning" => "callout callout-warning",
        "success" => "callout callout-success",
        _ => "callout callout-info"
    };
}
