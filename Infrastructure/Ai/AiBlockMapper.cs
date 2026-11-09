using Aplication.DTOs.ContentBlocks;

namespace Infrastructure.Ai;

/// <summary>Convierte bloques generados por la IA al contrato de bloques del portal.</summary>
internal static class AiBlockMapper
{
    public static List<ContentBlockDto> MapBlocks(List<AiBlock>? blocks)
    {
        var result = new List<ContentBlockDto>();
        if (blocks is null) return result;

        foreach (var b in blocks)
        {
            var dto = MapBlock(b);
            if (dto is not null) result.Add(dto);
        }

        return result;
    }

    private static ContentBlockDto? MapBlock(AiBlock b)
    {
        switch ((b.Type ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "paragraph":
                if (string.IsNullOrWhiteSpace(b.Text)) return null;
                return new ContentParagraphBlockDto { Text = b.Text };

            case "heading":
                if (string.IsNullOrWhiteSpace(b.Text)) return null;
                return new ContentHeadingBlockDto
                {
                    Level = b.Level is 2 or 3 ? b.Level.Value : 2,
                    Text = b.Text
                };

            case "bullet-list":
                var bulletItems = NonEmpty(b.Items);
                if (bulletItems.Count == 0) return null;
                return new ContentBulletListBlockDto { Items = bulletItems };

            case "ordered-list":
                var orderedItems = NonEmpty(b.Items);
                if (orderedItems.Count == 0) return null;
                return new ContentOrderedListBlockDto { Items = orderedItems };

            case "quote":
                if (string.IsNullOrWhiteSpace(b.Text)) return null;
                return new ContentQuoteBlockDto { Text = b.Text, Attribution = b.Attribution };

            case "callout":
                if (string.IsNullOrWhiteSpace(b.Text)) return null;
                var variant = (b.Variant ?? "info").Trim().ToLowerInvariant();
                if (variant is not ("info" or "tip" or "warning" or "success")) variant = "info";
                return new ContentCalloutBlockDto { Variant = variant, Title = b.Title, Text = b.Text };

            case "table":
                var headers = (b.Headers ?? []).Where(h => !string.IsNullOrWhiteSpace(h)).ToList();
                if (headers.Count == 0) return null;
                var rows = (b.Rows ?? []).Where(r => r != null).Select(r => r!).ToList();
                return new ContentTableBlockDto { Headers = headers, Rows = rows, Caption = b.Caption };

            case "code":
                if (string.IsNullOrWhiteSpace(b.Code)) return null;
                return new ContentCodeBlockDto { Code = b.Code, Language = b.Language };

            case "divider":
                return new ContentDividerBlockDto();

            default:
                return null;
        }
    }

    private static List<string> NonEmpty(List<string>? items) =>
        (items ?? []).Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
}
