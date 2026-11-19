using Domine.Entities;

namespace Aplication.Services;

/// <summary>
/// Valores predeterminados de longitud y apilamiento por plantilla. Es la única
/// fuente de números: la usan el prompt, el normalizador, los validadores y la UI.
/// </summary>
public static class ParagraphStyleDefaults
{
    /// <summary>Plantillas reconocidas por el generador.</summary>
    public static readonly IReadOnlyList<string> Templates =
        ["news", "informative", "comparative", "analysis", "guide", "tutorial", "faq", "ppa", "job"];

    private static readonly Dictionary<string, ParagraphStyleSetting> Defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["news"] = new() { MinWords = 40, MaxWords = 70, MaxConsecutive = 3 },
        ["informative"] = new() { MinWords = 60, MaxWords = 100, MaxConsecutive = 3 },
        ["comparative"] = new() { MinWords = 50, MaxWords = 90, MaxConsecutive = 3 },
        ["analysis"] = new() { MinWords = 60, MaxWords = 110, MaxConsecutive = 2 },
        ["guide"] = new() { MinWords = 40, MaxWords = 80, MaxConsecutive = 2 },
        ["tutorial"] = new() { MinWords = 40, MaxWords = 80, MaxConsecutive = 2 },
        ["faq"] = new() { MinWords = 70, MaxWords = 80, MaxConsecutive = 1 },
        ["ppa"] = new() { MinWords = 50, MaxWords = 90, MaxConsecutive = 2 },
        ["job"] = new() { MinWords = 40, MaxWords = 80, MaxConsecutive = 2 }
    };

    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["news"] = "Noticia",
        ["informative"] = "Informativa",
        ["comparative"] = "Comparativo",
        ["analysis"] = "Análisis",
        ["guide"] = "Guía práctica",
        ["tutorial"] = "Tutorial paso a paso",
        ["faq"] = "FAQ (preguntas frecuentes)",
        ["ppa"] = "PPA (People Also Ask)",
        ["job"] = "Convocatoria laboral"
    };

    /// <summary>Perfil usado cuando la plantilla no está reconocida.</summary>
    public static ParagraphStyleSetting Fallback => new() { MinWords = 50, MaxWords = 110, MaxConsecutive = 3 };

    public static string Normalize(string? template) =>
        string.IsNullOrWhiteSpace(template) ? "news" : template.Trim().ToLowerInvariant();

    public static string Label(string template) =>
        Labels.TryGetValue(template, out var label) ? label : template;

    /// <summary>Valores por defecto de una plantilla (copia, para no mutar el original).</summary>
    public static ParagraphStyleSetting For(string template) =>
        (Defaults.TryGetValue(template, out var setting) ? setting : Fallback).Clone();

    /// <summary>Todas las plantillas con sus valores por defecto.</summary>
    public static IReadOnlyDictionary<string, ParagraphStyleSetting> All()
    {
        var copy = new Dictionary<string, ParagraphStyleSetting>(StringComparer.OrdinalIgnoreCase);
        foreach (var template in Templates)
            copy[template] = For(template);
        return copy;
    }
}
