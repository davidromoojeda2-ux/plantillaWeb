using Domine.Entities;

namespace Aplication.Services;

/// <summary>
/// Estrategia de estilo para una plantilla: longitud, apilamiento y apertura.
/// Los valores provienen de la configuración y, si falta, de los predeterminados.
/// </summary>
public sealed record ParagraphStyleProfile(
    string Template,
    int MinWords,
    int MaxWords,
    int MaxConsecutive,
    int MaxFirstSentenceWords,
    IReadOnlyList<string> ForbiddenOpeners)
{
    /// <summary>Límite absoluto: por encima de este valor el párrafo es un defecto duro.</summary>
    public int HardMaxWords => (int)Math.Ceiling(MaxWords * 1.5);

    public static ParagraphStyleProfile Resolve(
        string? template,
        IReadOnlyDictionary<string, ParagraphStyleSetting>? overrides,
        string? language)
    {
        var key = ParagraphStyleDefaults.Normalize(template);
        var defaults = ParagraphStyleDefaults.For(key);
        var setting = Apply(defaults, overrides, key);

        return new ParagraphStyleProfile(
            key,
            setting.MinWords,
            setting.MaxWords,
            setting.MaxConsecutive,
            MaxFirstSentenceWords: 25,
            ParagraphOpeners.For(language));
    }

    private static ParagraphStyleSetting Apply(
        ParagraphStyleSetting defaults,
        IReadOnlyDictionary<string, ParagraphStyleSetting>? overrides,
        string key)
    {
        if (overrides is null || !overrides.TryGetValue(key, out var custom))
            return defaults;

        return Clamp(custom, defaults);
    }

    /// <summary>
    /// Ajusta un valor configurado a los límites admitidos. Si un campo viene vacío
    /// o fuera de rango, conserva el predeterminado de la plantilla.
    /// </summary>
    public static ParagraphStyleSetting Clamp(ParagraphStyleSetting? custom, ParagraphStyleSetting defaults)
    {
        if (custom is null) return defaults;

        var min = custom.MinWords is >= 20 and <= 200 ? custom.MinWords : defaults.MinWords;
        var max = custom.MaxWords is >= 40 and <= 300 ? custom.MaxWords : defaults.MaxWords;
        if (max <= min) max = Math.Min(300, min + 10);

        var consecutive = custom.MaxConsecutive is >= 1 and <= 6
            ? custom.MaxConsecutive
            : defaults.MaxConsecutive;

        return new ParagraphStyleSetting
        {
            MinWords = min,
            MaxWords = max,
            MaxConsecutive = consecutive
        };
    }
}
