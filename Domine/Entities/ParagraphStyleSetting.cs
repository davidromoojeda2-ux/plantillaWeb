namespace Domine.Entities;

/// <summary>
/// Umbrales de párrafo configurados para una plantilla editorial.
/// El máximo duro se deriva del máximo recomendado y no se persiste.
/// </summary>
public class ParagraphStyleSetting
{
    /// <summary>Mínimo de palabras recomendado por párrafo.</summary>
    public int MinWords { get; set; }

    /// <summary>Máximo de palabras recomendado por párrafo.</summary>
    public int MaxWords { get; set; }

    /// <summary>Párrafos consecutivos permitidos antes de pedir un recurso de respiro.</summary>
    public int MaxConsecutive { get; set; }

    public ParagraphStyleSetting Clone() => new()
    {
        MinWords = MinWords,
        MaxWords = MaxWords,
        MaxConsecutive = MaxConsecutive
    };
}
