namespace Aplication.Services;

/// <summary>
/// Fórmulas de relleno con las que la IA suele abrir párrafos en lugar de responder.
/// Se comparan en minúsculas y sin tildes contra el inicio de la primera frase.
/// </summary>
public static class ParagraphOpeners
{
    private static readonly Dictionary<string, string[]> ByLanguage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["es"] =
        [
            "en la actualidad", "hoy en dia", "cabe destacar", "es importante mencionar",
            "es importante destacar", "en el mundo de", "cuando hablamos de", "a la hora de",
            "sin duda", "conviene recordar", "en este sentido"
        ],
        ["en"] =
        [
            "in today's world", "nowadays", "it is important to note", "it is worth mentioning",
            "when it comes to", "in the world of", "needless to say", "in this sense",
            "it should be noted"
        ],
        ["pt"] =
        [
            "atualmente", "nos dias de hoje", "e importante destacar", "vale a pena mencionar",
            "quando falamos de", "no mundo de", "sem duvida", "convem lembrar"
        ],
        ["fr"] =
        [
            "de nos jours", "il est important de noter", "il convient de souligner",
            "quand on parle de", "dans le monde de", "sans aucun doute", "il faut rappeler"
        ],
        ["de"] =
        [
            "heutzutage", "es ist wichtig zu beachten", "es sei darauf hingewiesen",
            "wenn es um", "in der welt von", "zweifellos", "es sollte beachtet werden"
        ],
        ["it"] =
        [
            "al giorno d'oggi", "e importante notare", "vale la pena sottolineare",
            "quando si parla di", "nel mondo di", "senza dubbio", "va ricordato"
        ]
    };

    /// <summary>Devuelve las fórmulas del idioma indicado; vacío si no está soportado.</summary>
    public static IReadOnlyList<string> For(string? language)
    {
        var code = (language ?? string.Empty).Trim();
        if (code.Length > 2 && code.Contains('-')) code = code[..2];

        return ByLanguage.TryGetValue(code, out var openers)
            ? openers.Select(o => o.Trim().ToLowerInvariant()).Where(o => o.Length > 0).ToArray()
            : [];
    }
}
