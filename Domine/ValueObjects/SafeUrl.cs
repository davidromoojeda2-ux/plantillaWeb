using Domine.Content;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.ValueObjects;

/// <summary>
/// URL validada para recursos editoriales. Solo admite rutas relativas al sitio
/// o URLs absolutas HTTPS; la allowlist de hosts se aplica en aplicación.
/// </summary>
public sealed class SafeUrl
{
    [BsonElement("value")]
    public string Value { get; private set; } = string.Empty;

    [BsonIgnore]
    public string? Host { get; private set; }

    [BsonIgnore]
    public bool IsRelative { get; private set; }

    private SafeUrl()
    {
    }

    private SafeUrl(string value, string? host, bool isRelative)
    {
        Value = value;
        Host = host;
        IsRelative = isRelative;
    }

    public static SafeUrl CreateImageUrl(string? value, int maxLength = ContentLimits.MaxUrlLength)
    {
        var candidate = (value ?? string.Empty).Trim();
        if (candidate.Length == 0)
            throw new ArgumentException("La URL de la imagen es obligatoria.", nameof(value));

        if (candidate.Length > maxLength)
            throw new ArgumentOutOfRangeException(nameof(value), $"La URL supera el máximo de {maxLength} caracteres.");

        if (candidate.StartsWith("//", StringComparison.Ordinal) || candidate.Contains('\\'))
            throw new ArgumentException("La URL de la imagen no es válida.", nameof(value));

        if (candidate.StartsWith('/'))
        {
            return new SafeUrl(candidate, null, true);
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            throw new ArgumentException("La URL de la imagen no es válida.", nameof(value));

        if (uri.Scheme is not "https")
            throw new ArgumentException("La URL de la imagen debe usar https.", nameof(value));

        if (string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("La URL de la imagen no es válida.", nameof(value));

        return new SafeUrl(uri.ToString(), uri.Host, false);
    }
}
