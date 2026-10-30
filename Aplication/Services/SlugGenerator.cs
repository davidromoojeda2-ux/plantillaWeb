using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Aplication.Services;

/// <summary>Generación de slugs y normalización/hash de títulos para deduplicación.</summary>
public static class SlugGenerator
{
    /// <summary>Convierte un texto en un slug apto para URL (minúsculas, sin acentos).</summary>
    public static string Slugify(string? text)
    {
        var normalized = RemoveDiacritics(text ?? string.Empty).ToLowerInvariant();
        var value = Regex.Replace(normalized, @"[^a-z0-9]+", "-");
        return value.Trim('-');
    }

    /// <summary>Normaliza un título (minúsculas, sin acentos) para comparación.</summary>
    public static string Normalize(string? text) =>
        RemoveDiacritics(text ?? string.Empty).ToLowerInvariant();

    /// <summary>Calcula un hash estable del título normalizado.</summary>
    public static string Hash(string? text)
    {
        var normalized = Normalize(text);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString();
    }
}
