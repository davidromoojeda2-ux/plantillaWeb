using Domine.ContentBlocks;
using Domine.Entities;
using Domine.Exceptions;
using Domine.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ContenidoBloques.Tests;

public class DomainContentTests
{
    [Fact]
    public void BlockHierarchy_ExposesAllSupportedTypes()
    {
        var blocks = new List<ContentBlock>
        {
            new ContentParagraphBlock { Text = RichText.Create("<strong>Hola</strong>", "Hola") },
            new ContentHeadingBlock { Level = 2, Text = PlainText.Create("Título") },
            new ContentOrderedListBlock { Items = [RichText.Create("Uno", "Uno")] },
            new ContentBulletListBlock { Items = [RichText.Create("Dos", "Dos")] },
            new ContentQuoteBlock { Text = RichText.Create("Cita", "Cita") },
            new ContentCalloutBlock { Variant = CalloutVariant.Tip, Text = RichText.Create("Nota", "Nota") },
            new ContentImageBlock { Url = SafeUrl.CreateImageUrl("/uploads/foto.webp"), Alt = PlainText.Create("Foto") },
            new ContentTableBlock
            {
                Headers = [PlainText.Create("A"), PlainText.Create("B")],
                Rows = [[PlainText.Create("1"), PlainText.Create("2")]]
            },
            new ContentCodeBlock { Code = PlainText.Create("var x = 1;") },
            new ContentDividerBlock()
        };

        Assert.Equal(
            ["paragraph", "heading", "ordered-list", "bullet-list", "quote", "callout", "image", "table", "code", "divider"],
            blocks.Select(b => b.Type));
    }

    [Fact]
    public void ValueObjects_RejectInvalidLengthsAndSchemes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RichText.Create(new string('a', 11), "ok", maxHtmlLength: 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => PlainText.Create(new string('a', 11), maxLength: 10));
        Assert.Throws<ArgumentException>(() => SafeUrl.CreateImageUrl("javascript:alert(1)"));
        Assert.Throws<ArgumentException>(() => SafeUrl.CreateImageUrl("data:image/png;base64,AAAA"));
        Assert.Throws<ArgumentException>(() => SafeUrl.CreateImageUrl("//evil.example/foto.png"));
        Assert.Throws<ArgumentException>(() => SafeUrl.CreateImageUrl("http://images.example.com/foto.webp"));

        var relative = SafeUrl.CreateImageUrl("/uploads/foto.webp");
        var absolute = SafeUrl.CreateImageUrl("https://images.example.com/foto.webp");
        Assert.True(relative.IsRelative);
        Assert.Equal("images.example.com", absolute.Host);
    }

    [Fact]
    public void Article_BuildSearchText_UsesPlainTextAndExcludesMarkup()
    {
        var article = new Article
        {
            Title = "Artículo de prueba",
            Slug = "articulo-de-prueba",
            Blocks =
            [
                new ContentHeadingBlock { Level = 2, Text = PlainText.Create("Sección") },
                new ContentParagraphBlock { Text = RichText.Create("<strong>palabra</strong> clave", "palabra clave") },
                new ContentTableBlock
                {
                    Headers = [PlainText.Create("Columna")],
                    Rows = [[PlainText.Create("Celda")]]
                }
            ]
        };

        var searchText = article.BuildSearchText();

        Assert.Contains("palabra clave", searchText);
        Assert.Contains("Sección", searchText);
        Assert.Contains("Celda", searchText);
        Assert.DoesNotContain("<strong>", searchText);
        Assert.DoesNotContain("<", searchText);
    }

    [Fact]
    public void Article_Publish_RequiresBlocksAndCalculatesReadingTime()
    {
        var article = new Article { Title = "Título", Slug = "titulo" };

        Assert.Throws<DomainValidationException>(() => article.Publicar());

        article.Blocks = [new ContentParagraphBlock
        {
            Text = RichText.Create(
                string.Join(' ', Enumerable.Repeat("palabra", 400)),
                string.Join(' ', Enumerable.Repeat("palabra", 400)))
        }];

        article.Publicar();

        Assert.Equal(2, article.ReadingTimeMinutes);
        Assert.NotNull(article.PublishedAt);
        Assert.False(string.IsNullOrWhiteSpace(article.SearchText));
    }

    [Fact]
    public void BsonRoundTrip_PreservesBlockTypesAndOrder()
    {
        var article = new Article
        {
            Title = "Roundtrip",
            Slug = "roundtrip",
            Blocks =
            [
                new ContentParagraphBlock { Text = RichText.Create("<p>Hola</p>", "Hola") },
                new ContentOrderedListBlock { Items = [RichText.Create("Uno", "Uno"), RichText.Create("Dos", "Dos")] },
                new ContentImageBlock { Url = SafeUrl.CreateImageUrl("/uploads/foto.webp"), Alt = PlainText.Create("Foto") },
                new ContentTableBlock
                {
                    Headers = [PlainText.Create("A"), PlainText.Create("B")],
                    Rows = [[PlainText.Create("1"), PlainText.Create("2")]]
                },
                new ContentDividerBlock()
            ]
        };

        var document = article.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<Article>(document);

        Assert.Equal(article.Blocks.Select(b => b.Type), restored.Blocks.Select(b => b.Type));
        Assert.IsType<ContentParagraphBlock>(restored.Blocks[0]);
        Assert.Equal("Hola", ((ContentParagraphBlock)restored.Blocks[0]).Text.Text);
        Assert.Equal("Uno", ((ContentOrderedListBlock)restored.Blocks[1]).Items[0].Text);
        Assert.Equal("/uploads/foto.webp", ((ContentImageBlock)restored.Blocks[2]).Url!.Value);
        Assert.Equal("2", ((ContentTableBlock)restored.Blocks[3]).Rows[0][1].Value);
    }
}
