using Aplication.Configuration;
using Aplication.Content;
using Aplication.DTOs;
using Aplication.DTOs.ContentBlocks;
using Aplication.Mappers;
using Aplication.Services;
using Domine.ContentBlocks;
using Domine.Exceptions;
using Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace ContenidoBloques.Tests;

public class ContentBodySanitizerTests
{
    private static ContentBodySanitizer CreateSanitizer(Action<ContentSecurityOptions>? configure = null)
    {
        var options = new ContentSecurityOptions
        {
            MaxBlocks = 5,
            MaxJsonBytes = 1024 * 1024,
            MaxRichTextHtmlLength = 4000,
            MaxTextLength = 2000,
            MaxListItems = 3,
            MaxTableRows = 4,
            MaxTableColumns = 3,
            MaxUrlLength = 500,
            AllowedImageHosts = ["images.example.com"]
        };
        configure?.Invoke(options);
        return new ContentBodySanitizer(new GanssHtmlSanitizer(), Options.Create(options));
    }

    [Fact]
    public void Sanitize_ConvertsRichTextToDomainAndStripsDangerousMarkup()
    {
        var sanitizer = CreateSanitizer();

        var blocks = sanitizer.Sanitize(
        [
            new ContentParagraphBlockDto
            {
                Text = "<strong>Hola</strong><script>alert(1)</script> mundo"
            }
        ]);

        var paragraph = Assert.IsType<ContentParagraphBlock>(Assert.Single(blocks));
        Assert.Equal("Hola mundo", paragraph.Text.Text);
        Assert.DoesNotContain("<script", paragraph.Text.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RejectsTooManyBlocksAndEmptyParagraphs()
    {
        var sanitizer = CreateSanitizer(o => o.MaxBlocks = 1);

        Assert.Throws<DomainValidationException>(() => sanitizer.Sanitize(
        [
            new ContentParagraphBlockDto { Text = "uno" },
            new ContentParagraphBlockDto { Text = "dos" }
        ]));

        Assert.Throws<DomainValidationException>(() => sanitizer.Sanitize(
        [
            new ContentParagraphBlockDto { Text = "<br>" }
        ]));
    }

    [Fact]
    public void Sanitize_ValidatesImageHostsAndRequiresAlt()
    {
        var sanitizer = CreateSanitizer();

        var allowed = sanitizer.Sanitize(
        [
            new ContentImageBlockDto
            {
                Url = "https://images.example.com/foto.webp",
                Alt = "Foto permitida"
            }
        ]);
        Assert.IsType<ContentImageBlock>(Assert.Single(allowed));

        Assert.Throws<DomainValidationException>(() => sanitizer.Sanitize(
        [
            new ContentImageBlockDto
            {
                Url = "https://evil.example/foto.webp",
                Alt = "Foto bloqueada"
            }
        ]));

        Assert.Throws<DomainValidationException>(() => sanitizer.Sanitize(
        [
            new ContentImageBlockDto { Url = "/uploads/foto.webp" }
        ]));

        var relative = sanitizer.Sanitize(
        [
            new ContentImageBlockDto { Url = "/uploads/foto.webp", Alt = "Foto local" }
        ]);
        Assert.True(Assert.IsType<ContentImageBlock>(Assert.Single(relative)).Url!.IsRelative);
    }

    [Fact]
    public void Sanitize_ValidatesTableShapeAndLimits()
    {
        var sanitizer = CreateSanitizer();

        Assert.Throws<DomainValidationException>(() => sanitizer.Sanitize(
        [
            new ContentTableBlockDto
            {
                Headers = ["A", "B"],
                Rows = [["1"]]
            }
        ]));

        Assert.Throws<DomainValidationException>(() => sanitizer.Sanitize(
        [
            new ContentTableBlockDto
            {
                Headers = ["A", "B", "C", "D"]
            }
        ]));

        Assert.Throws<DomainValidationException>(() => sanitizer.Sanitize(
        [
            new ContentTableBlockDto
            {
                Headers = ["A"],
                Rows = [null!]
            }
        ]));

        var table = sanitizer.Sanitize(
        [
            new ContentTableBlockDto
            {
                Headers = ["A", "B"],
                Rows = [["1", "2"]],
                Caption = "Datos"
            }
        ]);
        var block = Assert.IsType<ContentTableBlock>(Assert.Single(table));
        Assert.Equal(2, block.Headers.Count);
        Assert.Equal("Datos", block.Caption!.Value);
    }

    [Fact]
    public void Sanitize_ValidatesCalloutVariantAndListLimits()
    {
        var sanitizer = CreateSanitizer();

        var callout = sanitizer.Sanitize(
        [
            new ContentCalloutBlockDto { Variant = "warning", Text = "Cuidado" }
        ]);
        Assert.Equal(CalloutVariant.Warning, Assert.IsType<ContentCalloutBlock>(Assert.Single(callout)).Variant);

        Assert.Throws<DomainValidationException>(() => sanitizer.Sanitize(
        [
            new ContentCalloutBlockDto { Variant = "fatal", Text = "Cuidado" }
        ]));

        Assert.Throws<DomainValidationException>(() => sanitizer.Sanitize(
        [
            new ContentBulletListBlockDto { Items = ["1", "2", "3", "4"] }
        ]));
    }

    [Fact]
    public void ImageResolver_FindsFirstBodyImageAndHandlesMissingImages()
    {
        Assert.False(ContentImageResolver.HasImage([new ContentParagraphBlockDto { Text = "Sin imagen" }]));
        Assert.Null(ContentImageResolver.FindFirstImage([new ContentDividerBlockDto()]));

        var image = new ContentImageBlockDto { ImageId = "507f1f77bcf86cd799439011", Alt = "Foto" };
        var blocks = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto { Text = "Antes" },
            image
        };

        Assert.True(ContentImageResolver.HasImage(blocks));
        Assert.Same(image, ContentImageResolver.FindFirstImage(blocks));
    }

    [Fact]
    public void Mapper_RoundTripsRichTextAndStructuredBlocks()
    {
        var sanitizer = CreateSanitizer();
        var domain = sanitizer.Sanitize(
        [
            new ContentHeadingBlockDto { Level = 2, Text = "Sección" },
            new ContentParagraphBlockDto { Text = "<em>Texto</em>" },
            new ContentImageBlockDto { Url = "/uploads/foto.webp", Alt = "Foto" },
            new ContentDividerBlockDto()
        ]);

        var dto = ContentBlockMapper.ToDtoList(domain);

        Assert.Collection(
            dto,
            b => Assert.Equal("Sección", Assert.IsType<ContentHeadingBlockDto>(b).Text),
            b => Assert.Equal("<em>Texto</em>", Assert.IsType<ContentParagraphBlockDto>(b).Text),
            b => Assert.Equal("/uploads/foto.webp", Assert.IsType<ContentImageBlockDto>(b).Url),
            b => Assert.IsType<ContentDividerBlockDto>(b));

        var articleDto = new ArticleDto
        {
            Title = "Título",
            Slug = "titulo",
            Summary = "Resumen válido de más de veinte caracteres.",
            Author = "Autor",
            Blocks = dto
        };

        var article = ArticleMapper.ToEntity(articleDto, domain);
        var response = ArticleMapper.ToDto(article);
        var edit = ArticleMapper.ToEditDto(response);

        Assert.Equal(4, response.Blocks.Count);
        Assert.Equal(4, edit.Blocks.Count);
        Assert.IsType<ContentParagraphBlockDto>(edit.Blocks[1]);
    }
}
