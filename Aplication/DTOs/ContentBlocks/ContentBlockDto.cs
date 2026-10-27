using System.Text.Json.Serialization;

namespace Aplication.DTOs.ContentBlocks;

/// <summary>
/// Contrato JSON del editor para un bloque editorial. El discriminador <c>type</c>
/// determina el subtipo y las propiedades desconocidas se rechazan.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ContentParagraphBlockDto), "paragraph")]
[JsonDerivedType(typeof(ContentHeadingBlockDto), "heading")]
[JsonDerivedType(typeof(ContentOrderedListBlockDto), "ordered-list")]
[JsonDerivedType(typeof(ContentBulletListBlockDto), "bullet-list")]
[JsonDerivedType(typeof(ContentQuoteBlockDto), "quote")]
[JsonDerivedType(typeof(ContentCalloutBlockDto), "callout")]
[JsonDerivedType(typeof(ContentImageBlockDto), "image")]
[JsonDerivedType(typeof(ContentTableBlockDto), "table")]
[JsonDerivedType(typeof(ContentCodeBlockDto), "code")]
[JsonDerivedType(typeof(ContentDividerBlockDto), "divider")]
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public abstract class ContentBlockDto
{
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ContentParagraphBlockDto : ContentBlockDto
{
    /// <summary>HTML inline; se sanitiza en servidor.</summary>
    public string Text { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ContentHeadingBlockDto : ContentBlockDto
{
    public int Level { get; set; } = 2;
    public string Text { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ContentOrderedListBlockDto : ContentBlockDto
{
    public List<string> Items { get; set; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ContentBulletListBlockDto : ContentBlockDto
{
    public List<string> Items { get; set; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ContentQuoteBlockDto : ContentBlockDto
{
    public string Text { get; set; } = string.Empty;
    public string? Attribution { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ContentCalloutBlockDto : ContentBlockDto
{
    public string Variant { get; set; } = "info";
    public string? Title { get; set; }
    public string Text { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ContentImageBlockDto : ContentBlockDto
{
    public string? ImageId { get; set; }
    public string? Url { get; set; }
    public string Alt { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? Credit { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ContentTableBlockDto : ContentBlockDto
{
    public List<string> Headers { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];
    public string? Caption { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ContentCodeBlockDto : ContentBlockDto
{
    public string Code { get; set; } = string.Empty;
    public string? Language { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ContentDividerBlockDto : ContentBlockDto
{
}
