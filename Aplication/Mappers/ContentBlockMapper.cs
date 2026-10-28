using Aplication.DTOs.ContentBlocks;
using Domine.ContentBlocks;

namespace Aplication.Mappers;

/// <summary>
/// Convierte bloques de dominio ya validados al contrato JSON de respuesta.
/// </summary>
public static class ContentBlockMapper
{
    public static List<ContentBlockDto> ToDtoList(IEnumerable<ContentBlock> blocks) =>
        blocks.Select(ToDto).ToList();

    public static ContentBlockDto ToDto(ContentBlock block) => block switch
    {
        ContentParagraphBlock paragraph => new ContentParagraphBlockDto
        {
            Text = paragraph.Text.Html
        },
        ContentHeadingBlock heading => new ContentHeadingBlockDto
        {
            Level = heading.Level,
            Text = heading.Text.Value
        },
        ContentOrderedListBlock ordered => new ContentOrderedListBlockDto
        {
            Items = ordered.Items.Select(i => i.Html).ToList()
        },
        ContentBulletListBlock bullets => new ContentBulletListBlockDto
        {
            Items = bullets.Items.Select(i => i.Html).ToList()
        },
        ContentQuoteBlock quote => new ContentQuoteBlockDto
        {
            Text = quote.Text.Html,
            Attribution = quote.Attribution?.Value
        },
        ContentCalloutBlock callout => new ContentCalloutBlockDto
        {
            Variant = callout.Variant.ToString().ToLowerInvariant(),
            Title = callout.Title?.Value,
            Text = callout.Text.Html
        },
        ContentImageBlock image => new ContentImageBlockDto
        {
            ImageId = image.ImageId,
            Url = image.Url?.Value,
            Alt = image.Alt.Value,
            Caption = image.Caption?.Value,
            Credit = image.Credit?.Value,
            Width = image.Width,
            Height = image.Height
        },
        ContentTableBlock table => new ContentTableBlockDto
        {
            Headers = table.Headers.Select(h => h.Value).ToList(),
            Rows = table.Rows.Select(row => row.Select(c => c.Value).ToList()).ToList(),
            Caption = table.Caption?.Value
        },
        ContentCodeBlock code => new ContentCodeBlockDto
        {
            Code = code.Code.Value,
            Language = code.Language?.Value
        },
        ContentDividerBlock => new ContentDividerBlockDto(),
        _ => throw new NotSupportedException($"Tipo de bloque no soportado: {block.GetType().Name}")
    };
}
