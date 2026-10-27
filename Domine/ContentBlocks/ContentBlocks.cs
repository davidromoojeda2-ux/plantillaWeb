using Domine.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.ContentBlocks;

/// <summary>
/// Bloque editorial tipado. Cada subtipo declara únicamente los campos de su
/// formato y se persiste como documento embebido con discriminador BSON.
/// </summary>
[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(
    typeof(ContentParagraphBlock),
    typeof(ContentHeadingBlock),
    typeof(ContentOrderedListBlock),
    typeof(ContentBulletListBlock),
    typeof(ContentQuoteBlock),
    typeof(ContentCalloutBlock),
    typeof(ContentImageBlock),
    typeof(ContentTableBlock),
    typeof(ContentCodeBlock),
    typeof(ContentDividerBlock))]
public abstract class ContentBlock
{
    [BsonIgnore]
    public abstract string Type { get; }
}

[BsonDiscriminator("paragraph")]
public sealed class ContentParagraphBlock : ContentBlock
{
    public RichText Text { get; set; } = RichText.Empty();

    public override string Type => "paragraph";
}

[BsonDiscriminator("heading")]
public sealed class ContentHeadingBlock : ContentBlock
{
    /// <summary>Nivel semántico permitido: 2 (H2) o 3 (H3).</summary>
    public int Level { get; set; } = 2;

    public PlainText Text { get; set; } = PlainText.Empty();

    public override string Type => "heading";
}

[BsonDiscriminator("ordered-list")]
public sealed class ContentOrderedListBlock : ContentBlock
{
    public List<RichText> Items { get; set; } = [];

    public override string Type => "ordered-list";
}

[BsonDiscriminator("bullet-list")]
public sealed class ContentBulletListBlock : ContentBlock
{
    public List<RichText> Items { get; set; } = [];

    public override string Type => "bullet-list";
}

[BsonDiscriminator("quote")]
public sealed class ContentQuoteBlock : ContentBlock
{
    public RichText Text { get; set; } = RichText.Empty();

    public PlainText? Attribution { get; set; }

    public override string Type => "quote";
}

[BsonDiscriminator("callout")]
public sealed class ContentCalloutBlock : ContentBlock
{
    public CalloutVariant Variant { get; set; } = CalloutVariant.Info;

    public PlainText? Title { get; set; }

    public RichText Text { get; set; } = RichText.Empty();

    public override string Type => "callout";
}

[BsonDiscriminator("image")]
public sealed class ContentImageBlock : ContentBlock
{
    public string? ImageId { get; set; }

    public SafeUrl? Url { get; set; }

    public PlainText Alt { get; set; } = PlainText.Empty();

    public PlainText? Caption { get; set; }

    public PlainText? Credit { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public override string Type => "image";
}

[BsonDiscriminator("table")]
public sealed class ContentTableBlock : ContentBlock
{
    public List<PlainText> Headers { get; set; } = [];

    public List<List<PlainText>> Rows { get; set; } = [];

    public PlainText? Caption { get; set; }

    public override string Type => "table";
}

[BsonDiscriminator("code")]
public sealed class ContentCodeBlock : ContentBlock
{
    public PlainText Code { get; set; } = PlainText.Empty();

    public PlainText? Language { get; set; }

    public override string Type => "code";
}

[BsonDiscriminator("divider")]
public sealed class ContentDividerBlock : ContentBlock
{
    public override string Type => "divider";
}

public enum CalloutVariant
{
    Info,
    Tip,
    Warning,
    Success
}
