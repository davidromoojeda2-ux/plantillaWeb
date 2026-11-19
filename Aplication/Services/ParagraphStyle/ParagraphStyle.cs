using System.Text;
using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>
/// Hallazgo agregado de estilo. Conserva los índices de los bloques afectados para
/// que la interfaz pueda llevar al usuario a cada punto observado.
/// </summary>
public sealed record ParagraphStyleIssue(
    string Code,
    string Message,
    IReadOnlyList<int> BlockIndexes);

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
        IReadOnlyList<ParagraphFinding> findings, ParagraphStyleProfile profile) =>
        SummarizeWithTargets(findings, profile).Select(issue => issue.Message).ToList();

    /// <summary>
    /// Igual que <see cref="Summarize"/>, pero conserva los bloques afectados por cada
    /// mensaje para que la interfaz pueda navegar al punto observado.
    /// </summary>
    public static IReadOnlyList<ParagraphStyleIssue> SummarizeWithTargets(
        IReadOnlyList<ParagraphFinding> findings, ParagraphStyleProfile profile)
    {
        if (findings.Count == 0) return [];

        var issues = new List<ParagraphStyleIssue>();

        var longParagraphs = findings
            .Where(f => f.Code == "length" && f.Value > profile.MaxWords)
            .ToList();
        if (longParagraphs.Count > 0)
        {
            var max = longParagraphs.Max(f => f.Value);
            issues.Add(new ParagraphStyleIssue(
                "style-length-long",
                longParagraphs.Count == 1
                    ? $"1 párrafo supera las {profile.MaxWords} palabras (el más largo: {max})."
                    : $"{longParagraphs.Count} párrafos superan las {profile.MaxWords} palabras (el más largo: {max}).",
                longParagraphs.Select(f => f.BlockIndex).Distinct().ToList()));
        }

        var shortParagraphs = findings
            .Where(f => f.Code == "length" && f.Value > 0 && f.Value < profile.MinWords)
            .ToList();
        if (shortParagraphs.Count > 0)
        {
            issues.Add(new ParagraphStyleIssue(
                "style-length-short",
                shortParagraphs.Count == 1
                    ? $"1 párrafo no alcanza las {profile.MinWords} palabras."
                    : $"{shortParagraphs.Count} párrafos no alcanzan las {profile.MinWords} palabras.",
                shortParagraphs.Select(f => f.BlockIndex).Distinct().ToList()));
        }

        var stacked = findings.Where(f => f.Code == "stacked").ToList();
        if (stacked.Count > 0)
        {
            issues.Add(new ParagraphStyleIssue(
                "style-stacked",
                $"Hay hasta {stacked.Max(f => f.Value)} párrafos seguidos sin un recurso de respiro (máximo {profile.MaxConsecutive}).",
                stacked.Select(f => f.BlockIndex).Distinct().ToList()));
        }

        var notAnswering = findings.Where(f => f.Code is "opener" or "first-sentence").ToList();
        if (notAnswering.Count > 0)
        {
            issues.Add(new ParagraphStyleIssue(
                "style-answer",
                notAnswering.Count == 1
                    ? "1 párrafo no responde de inmediato al encabezado (primera frase larga o con fórmula de relleno)."
                    : $"{notAnswering.Count} párrafos no responden de inmediato al encabezado (primera frase larga o con fórmula de relleno).",
                notAnswering.Select(f => f.BlockIndex).Distinct().ToList()));
        }

        return issues;
    }
}
