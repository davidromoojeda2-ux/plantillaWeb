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

    private static readonly Regex AnchorRegex = new(
        "<a\\b[^>]*\\bhref\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+))[^>]*>(.*?)</a\\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex HrefValueRegex = new(
        "(<a\\b[^>]*\\bhref\\s*=\\s*)(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+))",
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

        var (errors, warnings) = SplitBySeverity(issues);

        return new ArticleFormValidationResult(errors, warnings, ExtractLinks(blocks), issues);
    }

    /// <summary>Separa los mensajes por severidad, conservando el orden original.</summary>
    public static (IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) SplitBySeverity(
        IEnumerable<ArticleValidationIssue> issues)
    {
        var list = issues as IReadOnlyCollection<ArticleValidationIssue> ?? issues.ToList();

        return (
            list.Where(issue => issue.Severity == ArticleIssueSeverity.Error)
                .Select(issue => issue.Message)
                .ToList(),
            list.Where(issue => issue.Severity == ArticleIssueSeverity.Warning)
                .Select(issue => issue.Message)
                .ToList());
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
        IReadOnlyList<ContentBlockDto> blocks, string? url, Uri? baseUri = null)
    {
        if (string.IsNullOrWhiteSpace(url)) return [];

        var indexes = new List<int>();
        for (var i = 0; i < blocks.Count; i++)
        {
            if (BlockContainsLink(blocks[i], url, baseUri)) indexes.Add(i);
        }

        return indexes;
    }

    /// <summary>
    /// Elimina los enlaces a las URL indicadas conservando el texto visible. Se usa
    /// solo después de que el usuario confirme quitar enlaces caídos.
    /// </summary>
    public static void RemoveLinks(
        IReadOnlyList<ContentBlockDto> blocks,
        IReadOnlyCollection<string> brokenUrls,
        Uri? baseUri = null)
    {
        if (brokenUrls.Count == 0) return;

        var broken = brokenUrls
            .Select(url => CanonicalLink(url, baseUri))
            .Where(url => url is not null)
            .Select(url => url!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (broken.Count == 0) return;

        TransformHtmlFields(blocks, html => RemoveBrokenAnchors(html, broken, baseUri));
    }

    /// <summary>
    /// Sustituye el href de los enlaces indicados conservando el resto de la etiqueta
    /// y el texto visible. Devuelve cuántos enlaces se actualizaron.
    /// </summary>
    public static int ReplaceLinks(
        IReadOnlyList<ContentBlockDto> blocks,
        IReadOnlyDictionary<string, string> replacements,
        Uri? baseUri = null)
    {
        if (replacements.Count == 0) return 0;

        var map = replacements
            .Select(pair => (From: CanonicalLink(pair.Key, baseUri), To: pair.Value))
            .Where(pair => pair.From is not null && !string.IsNullOrWhiteSpace(pair.To))
            .GroupBy(pair => pair.From!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().To, StringComparer.OrdinalIgnoreCase);

        if (map.Count == 0) return 0;

        var count = 0;

        string Replace(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return html ?? string.Empty;

            return AnchorRegex.Replace(html, match =>
            {
                var raw = ExtractHrefValue(match);
                var canonical = CanonicalLink(WebUtility.HtmlDecode(raw).Trim(), baseUri);
                if (canonical is null || !map.TryGetValue(canonical, out var replacement))
                    return match.Value;

                count++;
                return SetAnchorHref(match.Value, replacement);
            });
        }

        TransformHtmlFields(blocks, Replace);

        return count;
    }

    /// <summary>
    /// Restaura el href original de los enlaces cuyo texto visible se conservó. Se
    /// aplica antes de mostrar la corrección: la IA no debe cambiar enlaces por su
    /// cuenta, solo proponer cambios que el usuario confirmará.
    /// </summary>
    public static int RestoreLinkHrefs(
        IReadOnlyList<ContentBlockDto> corrected,
        IReadOnlyList<ContentBlockDto> original)
    {
        var originals = new List<(string Text, string Href)>();

        foreach (var html in EnumerateHtml(original))
        {
            foreach (Match match in AnchorRegex.Matches(html))
            {
                var href = WebUtility.HtmlDecode(ExtractHrefValue(match)).Trim();
                var text = ParagraphText.Normalize(
                    ParagraphText.StripTags(match.Groups[4].Value));

                if (text.Length > 0 && href.Length > 0)
                    originals.Add((text, href));
            }
        }

        if (originals.Count == 0) return 0;

        var byText = originals
            .GroupBy(item => item.Text, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Href,
                StringComparer.OrdinalIgnoreCase);

        var count = 0;

        string Restore(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return html ?? string.Empty;

            return AnchorRegex.Replace(html, match =>
            {
                var text = ParagraphText.Normalize(
                    ParagraphText.StripTags(match.Groups[4].Value));

                if (!byText.TryGetValue(text, out var href)) return match.Value;

                var current = WebUtility.HtmlDecode(ExtractHrefValue(match)).Trim();
                if (string.Equals(current, href, StringComparison.Ordinal)) return match.Value;

                count++;
                return SetAnchorHref(match.Value, href);
            });
        }

        TransformHtmlFields(corrected, Restore);

        return count;
    }

    private static string ExtractHrefValue(Match match) =>
        match.Groups[1].Success
            ? match.Groups[1].Value
            : match.Groups[2].Success
                ? match.Groups[2].Value
                : match.Groups[3].Value;

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

        var empty = IndexesOf(blocks, ContentBlockRules.IsEmpty);
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

    private static bool BlockContainsLink(ContentBlockDto block, string url, Uri? baseUri)
    {
        var target = CanonicalLink(url, baseUri);

        foreach (var html in EnumerateHtml([block]))
        {
            foreach (var candidate in ExtractHrefs(html))
            {
                if (string.Equals(candidate, url, StringComparison.Ordinal)) return true;
                if (target is not null &&
                    string.Equals(CanonicalLink(candidate, baseUri), target, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Recorre todos los campos de texto editables de los bloques y aplica una
    /// transformación. Centraliza el recorrido que comparten quitar, reemplazar y
    /// restaurar enlaces.
    /// </summary>
    private static void TransformHtmlFields(
        IReadOnlyList<ContentBlockDto> blocks, Func<string, string> transform)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case ContentParagraphBlockDto p:
                    p.Text = transform(p.Text);
                    break;
                case ContentHeadingBlockDto h:
                    h.Text = transform(h.Text);
                    break;
                case ContentQuoteBlockDto q:
                    q.Text = transform(q.Text);
                    if (q.Attribution is not null) q.Attribution = transform(q.Attribution);
                    break;
                case ContentCalloutBlockDto c:
                    if (c.Title is not null) c.Title = transform(c.Title);
                    c.Text = transform(c.Text);
                    break;
                case ContentBulletListBlockDto l:
                    for (var i = 0; i < l.Items.Count; i++) l.Items[i] = transform(l.Items[i]);
                    break;
                case ContentOrderedListBlockDto l:
                    for (var i = 0; i < l.Items.Count; i++) l.Items[i] = transform(l.Items[i]);
                    break;
                case ContentTableBlockDto t:
                    for (var i = 0; i < t.Headers.Count; i++)
                        t.Headers[i] = transform(t.Headers[i]);
                    foreach (var row in t.Rows)
                        for (var i = 0; i < row.Count; i++)
                            row[i] = transform(row[i]);
                    break;
            }
        }
    }

    private static string RemoveBrokenAnchors(
        string? html, HashSet<string> broken, Uri? baseUri)
    {
        if (string.IsNullOrWhiteSpace(html) || broken.Count == 0) return html ?? string.Empty;

        return AnchorRegex.Replace(html, match =>
        {
            var raw = ExtractHrefValue(match);
            var canonical = CanonicalLink(WebUtility.HtmlDecode(raw).Trim(), baseUri);
            return canonical is not null && broken.Contains(canonical)
                ? match.Groups[4].Value
                : match.Value;
        });
    }

    /// <summary>Cambia solo el valor del href conservando el resto de la etiqueta.</summary>
    private static string SetAnchorHref(string anchorHtml, string href) =>
        HrefValueRegex.Replace(
            anchorHtml,
            match => match.Groups[1].Value + "\"" + WebUtility.HtmlEncode(href) + "\"",
            1);

    /// <summary>Forma canónica de un enlace para comparar hrefs y URLs normalizadas.</summary>
    public static string? CanonicalizeLink(string? link, Uri? baseUri = null) =>
        CanonicalLink(link, baseUri);

    /// <summary>
    /// Forma canónica de un enlace para comparar el href del contenido con la URL
    /// normalizada que devuelve el comprobador (sin fragmento ni barra final).
    /// </summary>
    private static string? CanonicalLink(string? link, Uri? baseUri)
    {
        if (string.IsNullOrWhiteSpace(link)) return null;

        var raw = WebUtility.HtmlDecode(link.Trim());
        if (raw.StartsWith('#')) return null;

        if (raw.StartsWith("//", StringComparison.Ordinal) && baseUri is not null)
            raw = $"{baseUri.Scheme}:{raw}";
        else if (raw.StartsWith("~/", StringComparison.Ordinal))
            raw = raw[1..];

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) &&
            (baseUri is null || !Uri.TryCreate(baseUri, raw, out uri)))
        {
            return raw.TrimEnd('/');
        }

        return $"{uri.Scheme.ToLowerInvariant()}://{uri.Host.ToLowerInvariant()}:{uri.Port}" +
               $"{uri.AbsolutePath.TrimEnd('/')}{uri.Query}";
    }

    private static IEnumerable<string> ExtractHrefs(string html)
    {
        foreach (Match match in HrefRegex.Matches(html))
        {
            var url = WebUtility.HtmlDecode(ExtractHrefValue(match)).Trim();
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
