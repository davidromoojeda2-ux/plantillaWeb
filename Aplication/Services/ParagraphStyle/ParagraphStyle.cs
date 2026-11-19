using System.Text;
using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>
/// Fachada del estilo de párrafo. Es la única superficie que consumen el generador,
/// el reescritor, los validadores y el panel: concentra las reglas y el texto que se
/// envía al modelo, de modo que prompt y validación no puedan desincronizarse.
/// </summary>
public static class ParagraphStyle
{
    private static readonly IParagraphRule[] Rules =
    [
        new ParagraphLengthRule(),
        new AnswerFirstRule(),
        new ParagraphStackingRule()
    ];

    /// <summary>Evalúa todas las reglas sobre los bloques y devuelve sus hallazgos.</summary>
    public static IReadOnlyList<ParagraphFinding> Analyze(
        IReadOnlyList<ContentBlockDto>? blocks, ParagraphStyleProfile profile)
    {
        if (blocks is null || blocks.Count == 0) return [];

        var context = new ParagraphContext(blocks, profile);
        return Rules.SelectMany(rule => rule.Evaluate(context)).ToList();
    }

    /// <summary>Reglas de párrafo en texto, derivadas del mismo perfil que valida.</summary>
    public static string DescribeForPrompt(ParagraphStyleProfile profile)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"- Cada \"paragraph\" debe tener entre {profile.MinWords} y {profile.MaxWords} palabras (nunca más de {profile.HardMaxWords}).");
        builder.AppendLine($"- El primer párrafo después de cada \"heading\" debe responder de inmediato: su primera frase (máximo {profile.MaxFirstSentenceWords} palabras) contiene la idea principal y el término clave.");

        if (profile.ForbiddenOpeners.Count > 0)
        {
            var openers = string.Join(", ", profile.ForbiddenOpeners.Select(o => $"«{o}»"));
            builder.AppendLine($"- No abras párrafos con fórmulas de relleno: {openers}.");
        }

        builder.Append($"- No encadenes más de {profile.MaxConsecutive} \"paragraph\" seguidos sin un \"heading\", una lista, una tabla o una cita; si un párrafo se alarga, divídelo en un corte natural.");
        return builder.ToString();
    }

    /// <summary>
    /// Resume los hallazgos en pocos mensajes para el panel de validación, evitando
    /// una advertencia por cada párrafo.
    /// </summary>
    public static IReadOnlyList<string> Summarize(
        IReadOnlyList<ParagraphFinding> findings, ParagraphStyleProfile profile)
    {
        if (findings.Count == 0) return [];

        var messages = new List<string>();

        var longParagraphs = findings
            .Where(f => f.Code == "length" && f.Value > profile.MaxWords)
            .ToList();
        if (longParagraphs.Count > 0)
        {
            var max = longParagraphs.Max(f => f.Value);
            messages.Add(longParagraphs.Count == 1
                ? $"1 párrafo supera las {profile.MaxWords} palabras (el más largo: {max})."
                : $"{longParagraphs.Count} párrafos superan las {profile.MaxWords} palabras (el más largo: {max}).");
        }

        var shortParagraphs = findings
            .Count(f => f.Code == "length" && f.Value > 0 && f.Value < profile.MinWords);
        if (shortParagraphs > 0)
        {
            messages.Add(shortParagraphs == 1
                ? $"1 párrafo no alcanza las {profile.MinWords} palabras."
                : $"{shortParagraphs} párrafos no alcanzan las {profile.MinWords} palabras.");
        }

        var stacked = findings.Where(f => f.Code == "stacked").ToList();
        if (stacked.Count > 0)
        {
            messages.Add($"Hay hasta {stacked.Max(f => f.Value)} párrafos seguidos sin un recurso de respiro (máximo {profile.MaxConsecutive}).");
        }

        var notAnswering = findings.Count(f => f.Code is "opener" or "first-sentence");
        if (notAnswering > 0)
        {
            messages.Add(notAnswering == 1
                ? "1 párrafo no responde de inmediato al encabezado (primera frase larga o con fórmula de relleno)."
                : $"{notAnswering} párrafos no responden de inmediato al encabezado (primera frase larga o con fórmula de relleno).");
        }

        return messages;
    }
}
