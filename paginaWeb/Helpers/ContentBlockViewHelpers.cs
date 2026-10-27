using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace paginaWeb.Helpers;

/// <summary>
/// Helpers de presentación para los bloques editoriales: ids deterministas de
/// encabezados, clases de callout e índice del bloque.
/// </summary>
public static class ContentBlockViewHelpers
{
    public static string HeadingId(string? text, int index)
    {
        var slug = Slugify(text);
        return string.IsNullOrEmpty(slug) ? $"seccion-{index}" : $"seccion-{slug}-{index}";
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

    public static int ContentBlockIndex(object? viewData) => viewData is int index ? index : 0;
}
