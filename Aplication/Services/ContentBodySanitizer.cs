using Aplication.Configuration;
using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Domine.Content;
using Domine.ContentBlocks;
using Domine.Exceptions;
using Domine.ValueObjects;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace Aplication.Services;

/// <summary>
/// Convierte el contrato del editor en bloques de dominio aplicando sanitización,
/// validación estructural y los límites configurados.
/// </summary>
public sealed class ContentBodySanitizer
{
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly ContentSecurityOptions _options;

    public ContentBodySanitizer(IHtmlSanitizer htmlSanitizer, IOptions<ContentSecurityOptions> options)
    {
        _htmlSanitizer = htmlSanitizer;
        _options = options.Value;
    }

    public IReadOnlyList<ContentBlock> Sanitize(IReadOnlyList<ContentBlockDto>? dtos)
    {
        if (dtos is null || dtos.Count == 0) return [];

        if (dtos.Count > _options.MaxBlocks)
            throw new DomainValidationException($"El cuerpo no puede superar los {_options.MaxBlocks} bloques.", "Blocks");

        var result = new List<ContentBlock>(dtos.Count);
        for (var index = 0; index < dtos.Count; index++)
        {
            try
            {
                result.Add(SanitizeBlock(dtos[index], index));
            }
            catch (DomainValidationException)
            {
                throw;
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or FormatException)
            {
                throw BlockError(index, ex.Message);
            }
        }

        return result;
    }

    private ContentBlock SanitizeBlock(ContentBlockDto dto, int index) => dto switch
    {
        ContentParagraphBlockDto paragraph => new ContentParagraphBlock
        {
            Text = SanitizeRichText(paragraph.Text, index, "párrafo", required: true)
        },
        ContentHeadingBlockDto heading => SanitizeHeading(heading, index),
        ContentOrderedListBlockDto ordered => new ContentOrderedListBlock
        {
            Items = SanitizeListItems(ordered.Items, index)
        },
        ContentBulletListBlockDto bullets => new ContentBulletListBlock
        {
            Items = SanitizeListItems(bullets.Items, index)
        },
        ContentQuoteBlockDto quote => new ContentQuoteBlock
        {
            Text = SanitizeRichText(quote.Text, index, "cita", required: true),
            Attribution = OptionalPlainText(quote.Attribution)
        },
        ContentCalloutBlockDto callout => SanitizeCallout(callout, index),
        ContentImageBlockDto image => SanitizeImage(image, index),
        ContentTableBlockDto table => SanitizeTable(table, index),
        ContentCodeBlockDto code => SanitizeCode(code, index),
        ContentDividerBlockDto => new ContentDividerBlock(),
        _ => throw BlockError(index, "El tipo de bloque no está soportado.")
    };

    private ContentHeadingBlock SanitizeHeading(ContentHeadingBlockDto dto, int index)
    {
        if (dto.Level is not (2 or 3))
            throw BlockError(index, "El encabezado debe ser de nivel H2 o H3.");

        var text = (dto.Text ?? string.Empty).Trim();
        if (text.Length == 0)
            throw BlockError(index, "El encabezado no puede estar vacío.");

        return new ContentHeadingBlock
        {
            Level = dto.Level,
            Text = PlainText.Create(text, _options.MaxTextLength)
        };
    }

    private List<RichText> SanitizeListItems(List<string>? items, int index)
    {
        if (items is null || items.Count == 0)
            throw BlockError(index, "La lista debe tener al menos un elemento.");

        if (items.Count > _options.MaxListItems)
            throw BlockError(index, $"La lista no puede superar los {_options.MaxListItems} elementos.");

        return items.Select(item => SanitizeRichText(item, index, "elemento de lista", required: true)).ToList();
    }

    private ContentCalloutBlock SanitizeCallout(ContentCalloutBlockDto dto, int index)
    {
        if (!Enum.TryParse<CalloutVariant>(dto.Variant, ignoreCase: true, out var variant) ||
            !Enum.IsDefined(variant))
        {
            throw BlockError(index, "La variante del callout debe ser info, tip, warning o success.");
        }

        return new ContentCalloutBlock
        {
            Variant = variant,
            Title = OptionalPlainText(dto.Title),
            Text = SanitizeRichText(dto.Text, index, "callout", required: true)
        };
    }

    private ContentImageBlock SanitizeImage(ContentImageBlockDto dto, int index)
    {
        var imageId = string.IsNullOrWhiteSpace(dto.ImageId) ? null : dto.ImageId.Trim();
        if (imageId is not null && !ObjectId.TryParse(imageId, out _))
            throw BlockError(index, "El identificador de imagen no es válido.");

        var url = string.IsNullOrWhiteSpace(dto.Url) ? null : dto.Url.Trim();
        if (imageId is null && url is null)
            throw BlockError(index, "La imagen debe tener un identificador de biblioteca o una URL.");

        SafeUrl? safeUrl = null;
        if (url is not null)
        {
            safeUrl = SafeUrl.CreateImageUrl(url, _options.MaxUrlLength);
            if (!safeUrl.IsRelative && !IsAllowedHost(safeUrl.Host))
                throw BlockError(index, $"El host de imagen '{safeUrl.Host}' no está permitido.");
        }

        if (dto.Width is <= 0 or > 20_000 || dto.Height is <= 0 or > 20_000)
            throw BlockError(index, "Las dimensiones de la imagen no son válidas.");

        var alt = (dto.Alt ?? string.Empty).Trim();
        if (alt.Length == 0)
            throw BlockError(index, "El texto alternativo de la imagen es obligatorio.");

        return new ContentImageBlock
        {
            ImageId = imageId,
            Url = safeUrl,
            Alt = PlainText.Create(alt, _options.MaxTextLength),
            Caption = OptionalPlainText(dto.Caption),
            Credit = OptionalPlainText(dto.Credit),
            Width = dto.Width,
            Height = dto.Height
        };
    }

    private ContentTableBlock SanitizeTable(ContentTableBlockDto dto, int index)
    {
        var headers = dto.Headers ?? [];
        var rows = dto.Rows ?? [];

        if (headers.Count == 0)
            throw BlockError(index, "La tabla debe tener al menos una columna.");

        if (headers.Count > _options.MaxTableColumns)
            throw BlockError(index, $"La tabla no puede superar las {_options.MaxTableColumns} columnas.");

        if (rows.Count + 1 > _options.MaxTableRows)
            throw BlockError(index, $"La tabla no puede superar las {_options.MaxTableRows} filas incluida la cabecera.");

        if (rows.Any(row => row is null || row.Count != headers.Count))
            throw BlockError(index, "Todas las filas deben tener la misma cantidad de columnas que la cabecera.");

        return new ContentTableBlock
        {
            Headers = headers.Select(h => PlainText.Create(h, _options.MaxTextLength)).ToList(),
            Rows = rows.Select(row => (row ?? []).Select(c => PlainText.Create(c, _options.MaxTextLength)).ToList()).ToList(),
            Caption = OptionalPlainText(dto.Caption)
        };
    }

    private ContentCodeBlock SanitizeCode(ContentCodeBlockDto dto, int index)
    {
        var code = (dto.Code ?? string.Empty).Trim();
        if (code.Length == 0)
            throw BlockError(index, "El bloque de código no puede estar vacío.");

        var language = (dto.Language ?? string.Empty).Trim().ToLowerInvariant();
        if (language.Length > 30 ||
            (language.Length > 0 && !language.All(c => char.IsLetterOrDigit(c) || c is '+' or '#' or '.' or '_' or '-')))
        {
            throw BlockError(index, "El lenguaje del código no es válido.");
        }

        return new ContentCodeBlock
        {
            Code = PlainText.Create(code, _options.MaxTextLength),
            Language = language.Length == 0 ? null : PlainText.Create(language, 30)
        };
    }

    private RichText SanitizeRichText(string? html, int index, string field, bool required)
    {
        var clean = _htmlSanitizer.Sanitize(html);
        var plain = _htmlSanitizer.ToPlainText(clean);

        if (required && plain.Length == 0)
            throw BlockError(index, $"El {field} no puede estar vacío.");

        return RichText.Create(clean, plain, _options.MaxRichTextHtmlLength, _options.MaxTextLength);
    }

    private PlainText? OptionalPlainText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : PlainText.Create(value, _options.MaxTextLength);

    private bool IsAllowedHost(string? host) =>
        !string.IsNullOrWhiteSpace(host) &&
        _options.AllowedImageHosts.Any(h => string.Equals(h, host, StringComparison.OrdinalIgnoreCase));

    private static DomainValidationException BlockError(int index, string message) =>
        new($"Bloque {index + 1}: {message}", "Blocks");
}
