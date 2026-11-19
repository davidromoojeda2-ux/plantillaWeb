using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>
/// R3: sin apilamiento. Detecta secuencias de párrafos consecutivos sin un recurso
/// de respiro (encabezado, lista, tabla, cita u otro bloque).
/// </summary>
internal sealed class ParagraphStackingRule : IParagraphRule
{
    public IEnumerable<ParagraphFinding> Evaluate(ParagraphContext context)
    {
        var profile = context.Profile;
        var runs = CollectRuns(context.Blocks);

        foreach (var run in runs)
        {
            if (run.Length <= profile.MaxConsecutive) continue;

            var severity = run.Length > profile.MaxConsecutive + 2
                ? ParagraphSeverity.Hard
                : ParagraphSeverity.Soft;

            var scope = string.IsNullOrWhiteSpace(run.Heading)
                ? string.Empty
                : $" en la sección «{run.Heading}»";

            yield return new ParagraphFinding(
                "stacked",
                severity,
                $"Hay {run.Length} párrafos seguidos sin encabezado, lista ni tabla{scope}; el máximo recomendado es {profile.MaxConsecutive + (profile.MaxConsecutive == 1 ? " párrafo" : " párrafos")}.",
                run.Start,
                run.Length);
        }
    }

    private static List<(int Start, int Length, string? Heading)> CollectRuns(
        IReadOnlyList<ContentBlockDto> blocks)
    {
        var runs = new List<(int, int, string?)>();
        string? heading = null;
        var start = -1;
        var length = 0;

        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] is ContentHeadingBlockDto current)
            {
                Flush(runs, start, length, heading);
                start = -1;
                length = 0;
                heading = ParagraphText.StripTags(current.Text).Trim();
                continue;
            }

            if (blocks[i] is ContentParagraphBlockDto)
            {
                if (length == 0) start = i;
                length++;
                continue;
            }

            Flush(runs, start, length, heading);
            start = -1;
            length = 0;
        }

        Flush(runs, start, length, heading);
        return runs;
    }

    private static void Flush(List<(int, int, string?)> runs, int start, int length, string? heading)
    {
        if (length > 0) runs.Add((start, length, heading));
    }
}
