using Domine.ValueObjects;

namespace Domine.ContentBlocks;

/// <summary>
/// Bloque editorial tipado. Cada subtipo declara únicamente los campos de su
/// formato y se persiste como documento embebido con discriminador.
/// El discriminador y la jerarquía se configuran en la capa de persistencia
/// (<c>Infrastructure</c>), no en el dominio.
/// </summary>
public abstract class ContentBlock
{
    public abstract string Type { get; }
}

public sealed class ContentParagraphBlock : ContentBlock
{
    public RichText Text { get; set; } = RichText.Empty();

    public override string Type => "paragraph";
}

public sealed class ContentHeadingBlock : ContentBlock
{
    /// <summary>Nivel semántico permitido: 2 (H2) o 3 (H3).</summary>
    public int Level { get; set; } = 2;

    public PlainText Text { get; set; } = PlainText.Empty();

    public override string Type => "heading";
}

public sealed class ContentOrderedListBlock : ContentBlock
{
    public List<RichText> Items { get; set; } = [];

    public override string Type => "ordered-list";
}

public sealed class ContentBulletListBlock : ContentBlock
{
    public List<RichText> Items { get; set; } = [];

    public override string Type => "bullet-list";
}

public sealed class ContentQuoteBlock : ContentBlock
{
    public RichText Text { get; set; } = RichText.Empty();

    public PlainText? Attribution { get; set; }

    public override string Type => "quote";
}

public sealed class ContentCalloutBlock : ContentBlock
{
    public CalloutVariant Variant { get; set; } = CalloutVariant.Info;

    public PlainText? Title { get; set; }

    public RichText Text { get; set; } = RichText.Empty();

    public override string Type => "callout";
}

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

public sealed class ContentDocumentBlock : ContentBlock
{
    /// <summary>Id del documento en la colección documents.</summary>
    public string? DocumentId { get; set; }

    /// <summary>Título visible opcional; si falta se usa el nombre del documento.</summary>
    public PlainText? Title { get; set; }

    /// <summary>Descripción visible opcional.</summary>
    public PlainText? Description { get; set; }

    public override string Type => "document";
}

public sealed class ContentTableBlock : ContentBlock
{
    public List<PlainText> Headers { get; set; } = [];

    public List<List<PlainText>> Rows { get; set; } = [];

    public PlainText? Caption { get; set; }

    public override string Type => "table";
}

public sealed class ContentCodeBlock : ContentBlock
{
    public PlainText Code { get; set; } = PlainText.Empty();

    public PlainText? Language { get; set; }

    public override string Type => "code";
}

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
