using Domine.Content;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.ValueObjects;

/// <summary>
/// Texto sin HTML. El renderizador lo codifica por defecto; nunca se interpreta
/// como marcado.
/// </summary>
public sealed class PlainText
{
    [BsonElement("value")]
    public string Value { get; private set; } = string.Empty;

    private PlainText()
    {
    }

    private PlainText(string value) => Value = value;

    public static PlainText Create(string? value, int maxLength = ContentLimits.MaxTextLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentOutOfRangeException(nameof(value), $"El texto supera el máximo de {maxLength} caracteres.");

        return new PlainText(normalized);
    }

    public static PlainText Empty() => new(string.Empty);
}
