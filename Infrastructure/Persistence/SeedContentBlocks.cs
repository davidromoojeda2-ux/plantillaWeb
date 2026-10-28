using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Domine.ContentBlocks;
using Domine.ValueObjects;
using Infrastructure.Security;

namespace Infrastructure.Persistence;

/// <summary>
/// Convierte HTML histórico en bloques tipados y sanitizados. Se usa para los datos
/// de prueba (seed) y para migrar el campo legado <c>Content</c> de las páginas
/// informativas. La conversión es de mejor esfuerzo: cubre los elementos soportados.
/// </summary>
public static class SeedContentBlocks
{
    public static List<ContentBlock> FromHtml(string html)
    {
        var sanitizer = new GanssHtmlSanitizer();
        var parser = new HtmlParser();
        var document = parser.ParseDocument($"<body>{html}</body>");
        var blocks = new List<ContentBlock>();

        if (document.Body is not null)
            Walk(document.Body, blocks, sanitizer);

        return blocks;
    }

    private static void Walk(INode node, List<ContentBlock> blocks, GanssHtmlSanitizer sanitizer)
    {
        foreach (var child in node.ChildNodes)
        {
            switch (child)
            {
                case IElement element:
                    AddElement(element, blocks, sanitizer);
                    break;
                case IText text when !string.IsNullOrWhiteSpace(text.Data):
                    var plain = text.Data.Trim();
                    if (plain.Length > 0)
                    {
                        blocks.Add(new ContentParagraphBlock
                        {
                            Text = RichText.Create(sanitizer.Sanitize(plain), plain)
                        });
                    }
                    break;
            }
        }
    }

    private static void AddElement(IElement element, List<ContentBlock> blocks, GanssHtmlSanitizer sanitizer)
    {
        var tag = element.LocalName;

        switch (tag)
        {
            case "h1":
            case "h2":
            case "h3":
                AddHeading(element, blocks, tag == "h3" ? 3 : 2);
                return;

            case "p":
                AddParagraph(element, blocks, sanitizer);
                return;

            case "ol":
                AddList(element, blocks, sanitizer, ordered: true);
                return;

            case "ul":
                AddList(element, blocks, sanitizer, ordered: false);
                return;

            case "blockquote":
                AddQuote(element, blocks, sanitizer);
                return;

            case "table":
                AddTable(element, blocks, sanitizer);
                return;

            case "figure":
                AddFigure(element, blocks, sanitizer);
                return;

            case "img":
                AddImage(element, null, blocks, sanitizer);
                return;

            case "pre":
                AddCode(element, blocks, sanitizer);
                return;

            case "hr":
                blocks.Add(new ContentDividerBlock());
                return;
        }

        if (tag == "div" && element.ClassList.Contains("callout"))
        {
            AddCallout(element, blocks, sanitizer);
            return;
        }

        // Wrappers como table-responsive-wrapper o contenedores de sección.
        Walk(element, blocks, sanitizer);
    }

    private static void AddHeading(IElement element, List<ContentBlock> blocks, int level)
    {
        var text = element.TextContent.Trim();
        if (text.Length > 0)
        {
            blocks.Add(new ContentHeadingBlock
            {
                Level = level,
                Text = PlainText.Create(text)
            });
        }
    }

    private static void AddParagraph(IElement element, List<ContentBlock> blocks, GanssHtmlSanitizer sanitizer)
    {
        var plain = element.TextContent.Trim();
        if (plain.Length == 0) return;

        var html = sanitizer.Sanitize(element.InnerHtml);
        blocks.Add(new ContentParagraphBlock
        {
            Text = RichText.Create(html, plain)
        });
    }

    private static void AddList(
        IElement element,
        List<ContentBlock> blocks,
        GanssHtmlSanitizer sanitizer,
        bool ordered)
    {
        var items = element.Children
            .Where(c => c.LocalName == "li")
            .Select(li => new { Html = sanitizer.Sanitize(li.InnerHtml), Plain = li.TextContent.Trim() })
            .Where(i => i.Plain.Length > 0)
            .Select(i => RichText.Create(i.Html, i.Plain))
            .ToList();

        if (items.Count == 0) return;

        if (ordered)
            blocks.Add(new ContentOrderedListBlock { Items = items });
        else
            blocks.Add(new ContentBulletListBlock { Items = items });
    }

    private static void AddQuote(IElement element, List<ContentBlock> blocks, GanssHtmlSanitizer sanitizer)
    {
        var clone = (IElement)element.Clone(true);
        var cite = clone.QuerySelector("cite");
        var attribution = cite?.TextContent.Trim();
        cite?.Remove();

        var plain = clone.TextContent.Trim();
        if (plain.Length == 0) return;

        blocks.Add(new ContentQuoteBlock
        {
            Text = RichText.Create(sanitizer.Sanitize(clone.InnerHtml), plain),
            Attribution = string.IsNullOrWhiteSpace(attribution) ? null : PlainText.Create(attribution)
        });
    }

    private static void AddCallout(IElement element, List<ContentBlock> blocks, GanssHtmlSanitizer sanitizer)
    {
        var variant = CalloutVariant.Info;
        if (element.ClassList.Contains("callout-warning")) variant = CalloutVariant.Warning;
        else if (element.ClassList.Contains("callout-success")) variant = CalloutVariant.Success;
        else if (element.ClassList.Contains("callout-tip")) variant = CalloutVariant.Tip;

        var body = element.QuerySelector(".callout-body") ?? element;
        var plain = body.TextContent.Trim();
        if (plain.Length == 0) return;

        blocks.Add(new ContentCalloutBlock
        {
            Variant = variant,
            Text = RichText.Create(sanitizer.Sanitize(body.InnerHtml), plain)
        });
    }

    private static void AddFigure(
        IElement figure,
        List<ContentBlock> blocks,
        GanssHtmlSanitizer sanitizer)
    {
        var image = figure.QuerySelector("img");
        if (image is null) return;

        var caption = figure.QuerySelector("figcaption")?.TextContent.Trim();
        AddImage(image, caption, blocks, sanitizer);
    }

    private static void AddImage(
        IElement image,
        string? caption,
        List<ContentBlock> blocks,
        GanssHtmlSanitizer sanitizer)
    {
        var source = image.GetAttribute("src");
        if (string.IsNullOrWhiteSpace(source)) return;

        SafeUrl safeUrl;
        try
        {
            safeUrl = SafeUrl.CreateImageUrl(source);
        }
        catch (ArgumentException)
        {
            return;
        }

        var alt = image.GetAttribute("alt")?.Trim();
        if (string.IsNullOrWhiteSpace(alt))
            alt = string.IsNullOrWhiteSpace(caption) ? "Imagen del artículo" : caption;

        var block = new ContentImageBlock
        {
            Url = safeUrl,
            Alt = PlainText.Create(alt),
            Caption = string.IsNullOrWhiteSpace(caption) ? null : PlainText.Create(caption)
        };

        if (int.TryParse(image.GetAttribute("width"), out var width) && width > 0)
            block.Width = width;

        if (int.TryParse(image.GetAttribute("height"), out var height) && height > 0)
            block.Height = height;

        blocks.Add(block);
    }

    private static void AddTable(
        IElement table,
        List<ContentBlock> blocks,
        GanssHtmlSanitizer sanitizer)
    {
        var headers = table
            .QuerySelectorAll("thead th")
            .Select(th => PlainText.Create(sanitizer.ToPlainText(th.InnerHtml)))
            .Where(t => t.Value.Length > 0)
            .ToList();

        if (headers.Count == 0) return;

        var rows = new List<List<PlainText>>();
        foreach (var row in table.QuerySelectorAll("tbody tr"))
        {
            var cells = row.Children
                .Where(c => c.LocalName is "td" or "th")
                .Select(c => PlainText.Create(sanitizer.ToPlainText(c.InnerHtml)))
                .ToList();

            if (cells.Count == headers.Count)
                rows.Add(cells);
        }

        blocks.Add(new ContentTableBlock
        {
            Headers = headers,
            Rows = rows
        });
    }

    private static void AddCode(
        IElement pre,
        List<ContentBlock> blocks,
        GanssHtmlSanitizer sanitizer)
    {
        var code = pre.QuerySelector("code") ?? pre;
        var text = code.TextContent.Trim();
        if (text.Length == 0) return;

        var language = code.ClassList
            .FirstOrDefault(c => c.StartsWith("language-", StringComparison.OrdinalIgnoreCase))?
            .Substring("language-".Length);

        blocks.Add(new ContentCodeBlock
        {
            Code = PlainText.Create(text),
            Language = string.IsNullOrWhiteSpace(language) ? null : PlainText.Create(language, 30)
        });
    }
}
