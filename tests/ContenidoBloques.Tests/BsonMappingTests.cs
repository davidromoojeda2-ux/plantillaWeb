using Domine.Common;
using Domine.ContentBlocks;
using Domine.Entities;
using Domine.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ContenidoBloques.Tests;

/// <summary>
/// Verifica que el mapeo BSON definido en <c>Infrastructure</c> conserve exactamente
/// el formato de almacenamiento que producían los antiguos atributos del dominio.
/// Protege contra migraciones de datos accidentales durante el desacoplamiento.
/// </summary>
public class BsonMappingTests
{
    [Fact]
    public void Article_IdIsObjectId_AndStatusIsString()
    {
        var article = new Article { Title = "t", Slug = "s" };

        var bson = article.ToBsonDocument();

        Assert.Equal(BsonType.ObjectId, bson["_id"].BsonType);
        Assert.Equal(article.Id, bson["_id"].AsObjectId.ToString());
        Assert.Equal(BsonType.String, bson["Status"].BsonType);
    }

    [Fact]
    public void Article_ForeignKeyIsObjectId()
    {
        var categoryId = EntityId.New();
        var article = new Article { CategoryId = categoryId };

        var bson = article.ToBsonDocument();

        Assert.Equal(BsonType.ObjectId, bson["CategoryId"].BsonType);
        Assert.Equal(categoryId, bson["CategoryId"].AsObjectId.ToString());
    }

    [Fact]
    public void Article_NullForeignKeyIsNull()
    {
        var bson = new Article().ToBsonDocument();

        Assert.Equal(BsonType.Null, bson["CategoryId"].BsonType);
        Assert.Equal(BsonType.Null, bson["SubcategoryId"].BsonType);
        Assert.Equal(BsonType.Null, bson["CoverImageId"].BsonType);
    }

    [Fact]
    public void Article_IdListsAreObjectIdArrays()
    {
        var imageId = EntityId.New();
        var documentId = EntityId.New();
        var article = new Article
        {
            ImageIds = [imageId],
            DocumentIds = [documentId]
        };

        var bson = article.ToBsonDocument();

        Assert.Equal(BsonType.ObjectId, bson["ImageIds"].AsBsonArray[0].BsonType);
        Assert.Equal(BsonType.ObjectId, bson["DocumentIds"].AsBsonArray[0].BsonType);
    }

    [Fact]
    public void Article_IdListsRoundTrip()
    {
        var imageId = EntityId.New();
        var documentId = EntityId.New();
        var article = new Article
        {
            Title = "t",
            Slug = "s",
            ImageIds = [imageId],
            DocumentIds = [documentId]
        };

        var restored = BsonSerializer.Deserialize<Article>(article.ToBsonDocument());

        Assert.Equal([imageId], restored.ImageIds);
        Assert.Equal([documentId], restored.DocumentIds);
    }

    [Fact]
    public void DocumentMetadata_IdIsObjectId()
    {
        var bson = new DocumentMetadata().ToBsonDocument();
        Assert.Equal(BsonType.ObjectId, bson["_id"].BsonType);
    }

    [Fact]
    public void Comment_StatusIsString()
    {
        var comment = new Comment { ArticleId = EntityId.New() };
        var bson = comment.ToBsonDocument();
        Assert.Equal(BsonType.String, bson["Status"].BsonType);
    }

    [Fact]
    public void SiteSettings_IdIsFixedString()
    {
        var bson = new SiteSettings().ToBsonDocument();

        Assert.Equal(BsonType.String, bson["_id"].BsonType);
        Assert.Equal(SiteSettings.SingletonId, bson["_id"].AsString);
    }

    [Fact]
    public void ContentBlock_WritesTypeDiscriminator()
    {
        var article = new Article
        {
            Blocks =
            [
                new ContentParagraphBlock { Text = RichText.Create("hola", "hola") }
            ]
        };

        var bson = article.ToBsonDocument();
        var block = bson["Blocks"].AsBsonArray[0].AsBsonDocument;

        // Con la clase raíz, el discriminador se serializa como la cadena de tipos.
        var discriminator = block["_t"];
        Assert.Equal(BsonType.Array, discriminator.BsonType);
        Assert.Equal(["ContentBlock", "paragraph"], discriminator.AsBsonArray.Select(v => v.AsString));
    }

    [Fact]
    public void ContentBlock_RoundTripsDiscriminatorsAndValues()
    {
        var article = new Article
        {
            Title = "t",
            Slug = "s",
            Blocks =
            [
                new ContentParagraphBlock { Text = RichText.Create("<p>Hola</p>", "Hola") },
                new ContentHeadingBlock { Level = 3, Text = PlainText.Create("Sección") }
            ]
        };

        var restored = BsonSerializer.Deserialize<Article>(article.ToBsonDocument());

        var paragraph = Assert.IsType<ContentParagraphBlock>(restored.Blocks[0]);
        Assert.Equal("Hola", paragraph.Text.Text);
        var heading = Assert.IsType<ContentHeadingBlock>(restored.Blocks[1]);
        Assert.Equal(3, heading.Level);
        Assert.Equal("Sección", heading.Text.Value);
    }
}
