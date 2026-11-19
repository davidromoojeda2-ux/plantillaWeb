using System.Net;
using System.Text.RegularExpressions;
using Aplication.DTOs;
using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>
/// Valida la información editorial de un artículo del panel (campos obligatorios,
/// estructura de bloques y enlaces presentes en el contenido). Cada hallazgo incluye
/// el campo o los bloques a los que apunta para que el editor pueda navegar hasta él.
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
        IReadOnlyList<ContentBlockDto> blocks,
        ParagraphStyleProfile? paragraphStyle = null)
    {
        var issues = new List<ArticleValidationIssue>();

        ValidateRequiredText(title, 5, 250, "El título", "titleInput", issues);
        ValidateRequiredText(summary, 20, 500, "El resumen", "Summary", issues);
        ValidateRequiredText(author, 1, 100, "La firma o autor", "Author", issues);
        ValidateSlug(slug, issues);
        ValidateTaxonomy(categoryId, coverImageUrl, status, issues);
        ValidateBlocks(blocks, issues);

        if (paragraphStyle is not null && blocks.Count > 0)
        {
            var findings = ParagraphStyle.Analyze(blocks, paragraphStyle);
            foreach (var styleIssue in ParagraphStyle.SummarizeWithTargets(findings, paragraphStyle))
            {
                issues.Add(new ArticleValidationIssue(
                    ArticleIssueSeverity.Warning,
                    styleIssue.Code,
                    styleIssue.Message,
                    BlockIndexes: styleIssue.BlockIndexes));
            }
        }

        var errors = issues
            .Where(issue => issue.Severity == ArticleIssueSeverity.Error)
            .Select(issue => issue.Message)
            .ToList();
        var warnings = issues
            .Where(issue => issue.Severity == ArticleIssueSeverity.Warning)
            .Select(issue => issue.Message)
            .ToList();

        return new ArticleFormValidationResult(errors, warnings, ExtractLinks(blocks), issues);
    }

    /// <summary>Extrae los enlaces (href) del contenido evitando duplicados.</summary>
    public static IReadOnlyList<string> ExtractLinks(IReadOnlyList<ContentBlockDto> blocks)
    {
        var links = new List<string>();

        foreach (var html in EnumerateHtml(blocks))
        {
            foreach (var url in ExtractHrefs(html))
            {
                if (links.Contains(url, StringComparer.Ordinal)) continue;
                links.Add(url);
            }
        }

        return links;
    }

    /// <summary>Bloques que contienen un enlace con la URL indicada.</summary>
    public static IReadOnlyList<int> FindBlocksWithLink(
        IReadOnlyList<ContentBlockDto> blocks, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return [];

        var indexes = new List<int>();
        for (var i = 0; i < blocks.Count; i++)
        {
            if (BlockContainsLink(blocks[i], url)) indexes.Add(i);
        }

        return indexes;
    }

    private static void ValidateRequiredText(
        string? value, int minLength, int maxLength,
        string label, string field, List<ArticleValidationIssue> issues)
    {
        var text = value?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            AddError(issues, "required", $"{label} es obligatorio.", field);
        }
        else if (text.Length < minLength)
        {
            AddError(issues, "length", $"{label} debe tener al menos {minLength} caracteres.", field);
        }
        else if (text.Length > maxLength)
        {
            AddError(issues, "length", $"{label} no puede superar los {maxLength} caracteres.", field);
        }
    }

    private static void ValidateSlug(string? slug, List<ArticleValidationIssue> issues)
    {
        var slugValue = slug?.Trim() ?? string.Empty;

        if (slugValue.Length == 0)
        {
            AddError(issues, "slug", "El slug es obligatorio.", "slugInput");
        }
        else if (slugValue.Length > 200)
        {
            AddError(issues, "slug", "El slug no puede superar los 200 caracteres.", "slugInput");
        }
        else if (!SlugRegex.IsMatch(slugValue))
        {
            AddError(issues, "slug",
                "El slug solo admite minúsculas, números y guiones simples (sin espacios, mayúsculas ni acentos).",
                "slugInput");
        }
    }

    private static void ValidateTaxonomy(
        string? categoryId, string? coverImageUrl, string? status,
        List<ArticleValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            AddWarning(issues, "category", "Sin categoría principal asignada.", "categorySelect");

        if (string.IsNullOrWhiteSpace(coverImageUrl))
            AddWarning(issues, "cover", "Sin imagen de portada asignada.", "coverImageUrlInput");

        if (!string.IsNullOrWhiteSpace(status) &&
            !string.Equals(status, "Publicado", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(status, "Borrador", StringComparison.OrdinalIgnoreCase))
        {
            AddWarning(issues, "status",
                $"El estado «{status}» no es un estado editorial reconocido.", "Status");
        }
    }

    private static void ValidateBlocks(
        IReadOnlyList<ContentBlockDto> blocks, List<ArticleValidationIssue> issues)
    {
        if (blocks.Count == 0)
        {
            AddError(issues, "blocks", "El artículo no contiene bloques de contenido.",
                "contentBlocksEditor");
            return;
        }

        if (blocks[0] is ContentHeadingBlockDto)
        {
            AddError(issues, "hierarchy",
                "El artículo no debe empezar con un encabezado; comienza con un párrafo introductorio.",
                blockIndexes: [0]);
        }

        var levelOne = IndexesOf(blocks, block => block is ContentHeadingBlockDto { Level: 1 });
        if (levelOne.Count > 0)
        {
            AddError(issues, "hierarchy",
                "No uses encabezados de nivel 1; las secciones deben usar nivel 2 o 3.",
                blockIndexes: levelOne);
        }

        var empty = IndexesOf(blocks, IsEmptyBlock);
        if (empty.Count > 0)
        {
            AddError(issues, "empty",
                "Hay bloques vacíos o listas con menos de 2 elementos; completa o elimina esos bloques.",
                blockIndexes: empty);
        }

        for (var i = 1; i < blocks.Count; i++)
        {
            if (blocks[i - 1] is ContentHeadingBlockDto previous &&
                blocks[i] is ContentHeadingBlockDto current &&
                previous.Level == current.Level)
            {
                AddWarning(issues, "heading-run",
                    "Hay dos encabezados del mismo nivel consecutivos; añade contenido entre ellos.",
                    blockIndexes: [i]);
                break;
            }
        }

        if (blocks.Count < 3)
        {
            AddWarning(issues, "blocks",
                "El artículo tiene menos de 3 bloques; considera ampliar el contenido.");
        }

        var tooDeep = IndexesOf(blocks, block => block is ContentHeadingBlockDto { Level: > 3 });
        if (tooDeep.Count > 0)
        {
            AddWarning(issues, "hierarchy",
                "Hay encabezados de nivel 4 o superior; usa nivel 2 o 3 para las secciones.",
                blockIndexes: tooDeep);
        }
    }

    private static void AddError(
        List<ArticleValidationIssue> issues, string code, string message,
        string? field = null, IReadOnlyList<int>? blockIndexes = null) =>
        issues.Add(new ArticleValidationIssue(
            ArticleIssueSeverity.Error, code, message, field, blockIndexes));

    private static void AddWarning(
        List<ArticleValidationIssue> issues, string code, string message,
        string? field = null, IReadOnlyList<int>? blockIndexes = null) =>
        issues.Add(new ArticleValidationIssue(
            ArticleIssueSeverity.Warning, code, message, field, blockIndexes));

    private static List<int> IndexesOf(
        IReadOnlyList<ContentBlockDto> blocks, Func<ContentBlockDto, bool> predicate)
    {
        var indexes = new List<int>();
        for (var i = 0; i < blocks.Count; i++)
        {
            if (predicate(blocks[i])) indexes.Add(i);
        }
        return indexes;
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

    private static bool BlockContainsLink(ContentBlockDto block, string url)
    {
        foreach (var html in EnumerateHtml([block]))
        {
            foreach (var candidate in ExtractHrefs(html))
            {
                if (string.Equals(candidate, url, StringComparison.Ordinal)) return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> ExtractHrefs(string html)
    {
        foreach (Match match in HrefRegex.Matches(html))
        {
            var raw = match.Groups[1].Success
                ? match.Groups[1].Value
                : match.Groups[2].Success
                    ? match.Groups[2].Value
                    : match.Groups[3].Value;

            var url = WebUtility.HtmlDecode(raw).Trim();
            if (IsCheckableLink(url)) yield return url;
        }
    }

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
