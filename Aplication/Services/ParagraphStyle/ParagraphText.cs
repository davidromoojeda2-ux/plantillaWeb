using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Aplication.Services;

/// <summary>Utilidades de texto compartidas por reglas, normalizador y validadores.</summary>
internal static class ParagraphText
{
    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WordRegex = new(@"\S+", RegexOptions.Compiled);
    private static readonly Regex SentenceRegex = new(@"(?<=[\.!\?…])\s+", RegexOptions.Compiled);

    public static string StripTags(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : TagRegex.Replace(text, " ");

    /// <summary>Indica si el texto trae marcado inline (enlaces, negritas) que no conviene partir.</summary>
    public static bool HasInlineMarkup(string? text) =>
        !string.IsNullOrEmpty(text) && text.Contains('<');

    public static int CountWords(string? text)
    {
        var plain = StripTags(text);
        return plain.Length == 0 ? 0 : WordRegex.Matches(plain).Count;
    }

    public static IReadOnlyList<string> SplitSentences(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        return SentenceRegex.Split(text)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();
    }

    public static string FirstSentence(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var end = text.IndexOfAny(['.', '!', '?', '…']);
        return end < 0 ? text.Trim() : text[..(end + 1)].Trim();
    }

    /// <summary>Minúsculas, sin tildes y con espacios colapsados, para comparar aperturas.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.IsWhiteSpace(c) ? ' ' : c);
        }

        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
    }
}
