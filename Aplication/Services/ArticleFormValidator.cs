using System.Net;
using System.Text.RegularExpressions;
using Aplication.DTOs;
using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>
/// Valida la información editorial de un artículo del panel (campos obligatorios,
/// estructura de bloques y enlaces presentes en el contenido).
/// </summary>
public static class ArticleFormValidator
{
    private static readonly Regex SlugRegex = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    private static readonly Regex HrefRegex = new(
        "<a\\b[^>]*\\bhref\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static ArticleFormValidationResult Validate(
        string? title,
        string? summary,
        string? slug,
        string? author,
        string? categoryId,
        string? coverImageUrl,
        string? status,
        IReadOnlyList<ContentBlockDto> blocks)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        ValidateRequiredText(title, 5, 250, "El título", errors);
        ValidateRequiredText(summary, 20, 500, "El resumen", errors);
        ValidateRequiredText(author, 1, 100, "La firma o autor", errors);

        var slugValue = slug?.Trim() ?? string.Empty;
        if (slugValue.Length == 0)
        {
            errors.Add("El slug es obligatorio.");
        }
        else if (slugValue.Length > 200)
        {
            errors.Add("El slug no puede superar los 200 caracteres.");
        }
        else if (!SlugRegex.IsMatch(slugValue))
        {
            errors.Add("El slug solo admite minúsculas, números y guiones simples (sin espacios, mayúsculas ni acentos).");
        }

        if (string.IsNullOrWhiteSpace(categoryId))
            warnings.Add("Sin categoría principal asignada.");

        if (string.IsNullOrWhiteSpace(coverImageUrl))
            warnings.Add("Sin imagen de portada asignada.");

        if (!string.IsNullOrWhiteSpace(status) &&
            !string.Equals(status, "Publicado", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(status, "Borrador", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"El estado «{status}» no es un estado editorial reconocido.");
        }

        ValidateBlocks(blocks, errors, warnings);

        return new ArticleFormValidationResult(errors, warnings, ExtractLinks(blocks));
    }

    /// <summary>Extrae los enlaces (href) del contenido evitando duplicados.</summary>
    public static IReadOnlyList<string> ExtractLinks(IReadOnlyList<ContentBlockDto> blocks)
    {
        var links = new List<string>();

        foreach (var html in EnumerateHtml(blocks))
        {
            foreach (Match match in HrefRegex.Matches(html))
            {
                var raw = match.Groups[1].Success
                    ? match.Groups[1].Value
                    : match.Groups[2].Success
                        ? match.Groups[2].Value
                        : match.Groups[3].Value;

                var url = WebUtility.HtmlDecode(raw).Trim();
                if (!IsCheckableLink(url)) continue;
                if (links.Contains(url, StringComparer.Ordinal)) continue;
                links.Add(url);
            }
        }

        return links;
    }

    private static void ValidateRequiredText(
        string? value, int minLength, int maxLength, string label, List<string> errors)
    {
        var text = value?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            errors.Add($"{label} es obligatorio.");
        }
        else if (text.Length < minLength)
        {
            errors.Add($"{label} debe tener al menos {minLength} caracteres.");
        }
        else if (text.Length > maxLength)
        {
            errors.Add($"{label} no puede superar los {maxLength} caracteres.");
        }
    }

    private static void ValidateBlocks(
        IReadOnlyList<ContentBlockDto> blocks, List<string> errors, List<string> warnings)
    {
        if (blocks.Count == 0)
        {
            errors.Add("El artículo no contiene bloques de contenido.");
            return;
        }

        if (blocks[0] is ContentHeadingBlockDto)
            errors.Add("El artículo no debe empezar con un encabezado; comienza con un párrafo introductorio.");

        if (blocks.Any(b => b is ContentHeadingBlockDto { Level: 1 }))
            errors.Add("No uses encabezados de nivel 1; las secciones deben usar nivel 2 o 3.");

        if (blocks.Any(IsEmptyBlock))
            errors.Add("Hay bloques vacíos o listas con menos de 2 elementos; completa o elimina esos bloques.");

        for (var i = 1; i < blocks.Count; i++)
        {
            if (blocks[i - 1] is ContentHeadingBlockDto previous &&
                blocks[i] is ContentHeadingBlockDto current &&
                previous.Level == current.Level)
            {
                warnings.Add("Hay dos encabezados del mismo nivel consecutivos; añade contenido entre ellos.");
                break;
            }
        }

        if (blocks.Count < 3)
            warnings.Add("El artículo tiene menos de 3 bloques; considera ampliar el contenido.");

        if (blocks.Any(b => b is ContentHeadingBlockDto { Level: > 3 }))
            warnings.Add("Hay encabezados de nivel 4 o superior; usa nivel 2 o 3 para las secciones.");
    }

    private static bool IsEmptyBlock(ContentBlockDto block) => block switch
    {
        ContentParagraphBlockDto p => string.IsNullOrWhiteSpace(p.Text),
        ContentHeadingBlockDto h => string.IsNullOrWhiteSpace(h.Text),
        ContentQuoteBlockDto q => string.IsNullOrWhiteSpace(q.Text),
        ContentCalloutBlockDto c => string.IsNullOrWhiteSpace(c.Text),
        ContentBulletListBlockDto l => l.Items.Count < 2 || l.Items.All(string.IsNullOrWhiteSpace),
        ContentOrderedListBlockDto l => l.Items.Count < 2 || l.Items.All(string.IsNullOrWhiteSpace),
        ContentTableBlockDto t => t.Headers.Count == 0 || t.Rows.Count < 2,
        ContentCodeBlockDto c => string.IsNullOrWhiteSpace(c.Code),
        _ => false
    };

    private static IEnumerable<string> EnumerateHtml(IReadOnlyList<ContentBlockDto> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case ContentParagraphBlockDto p:
                    yield return p.Text;
                    break;
                case ContentHeadingBlockDto h:
                    yield return h.Text;
                    break;
                case ContentQuoteBlockDto q:
                    if (!string.IsNullOrWhiteSpace(q.Text)) yield return q.Text;
                    if (!string.IsNullOrWhiteSpace(q.Attribution)) yield return q.Attribution;
                    break;
                case ContentCalloutBlockDto c:
                    if (!string.IsNullOrWhiteSpace(c.Title)) yield return c.Title;
                    if (!string.IsNullOrWhiteSpace(c.Text)) yield return c.Text;
                    break;
                case ContentBulletListBlockDto l:
                    foreach (var item in l.Items)
                        if (!string.IsNullOrWhiteSpace(item)) yield return item;
                    break;
                case ContentOrderedListBlockDto l:
                    foreach (var item in l.Items)
                        if (!string.IsNullOrWhiteSpace(item)) yield return item;
                    break;
                case ContentTableBlockDto t:
                    foreach (var header in t.Headers)
                        if (!string.IsNullOrWhiteSpace(header)) yield return header;
                    foreach (var row in t.Rows)
                        foreach (var cell in row)
                            if (!string.IsNullOrWhiteSpace(cell)) yield return cell;
                    break;
            }
        }
    }

    private static bool IsCheckableLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (url.StartsWith('#')) return false;
        if (url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("ftp:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            return absolute.Scheme is "http" or "https";

        // Enlaces relativos del propio sitio: /ruta, ~/ruta o ruta/sin-esquema.
        return !url.Contains("://", StringComparison.Ordinal);
    }
}
