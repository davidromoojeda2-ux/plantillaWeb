using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>
/// Corrección determinista: divide los párrafos que superan el máximo en límites de
/// frase. No toca párrafos con marcado inline para no romper enlaces o negritas.
/// </summary>
public static class ParagraphNormalizer
{
    private const int MinChunkWords = 20;

    public static void Apply(List<ContentBlockDto>? blocks, ParagraphStyleProfile profile)
    {
        if (blocks is null || blocks.Count == 0) return;

        var result = new List<ContentBlockDto>(blocks.Count);
        var changed = false;

        foreach (var block in blocks)
        {
            if (block is not ContentParagraphBlockDto paragraph
                || ParagraphText.HasInlineMarkup(paragraph.Text)
                || ParagraphText.CountWords(paragraph.Text) <= profile.MaxWords)
            {
                result.Add(block);
                continue;
            }

            var chunks = Split(paragraph.Text, profile);
            if (chunks.Count <= 1)
            {
                result.Add(block);
                continue;
            }

            changed = true;
            result.AddRange(chunks.Select(text => new ContentParagraphBlockDto { Text = text }));
        }

        if (!changed) return;

        blocks.Clear();
        blocks.AddRange(result);
    }

    private static List<string> Split(string text, ParagraphStyleProfile profile)
    {
        var sentences = ParagraphText.SplitSentences(text);
        var chunks = new List<string>();
        var buffer = new List<string>();
        var words = 0;

        foreach (var sentence in sentences)
        {
            var sentenceWords = ParagraphText.CountWords(sentence);

            if (words > 0 && words + sentenceWords > profile.MaxWords)
            {
                chunks.Add(string.Join(' ', buffer));
                buffer.Clear();
                words = 0;
            }

            buffer.Add(sentence);
            words += sentenceWords;
        }

        if (buffer.Count > 0)
            chunks.Add(string.Join(' ', buffer));

        return Rebalance(chunks);
    }

    /// <summary>Evita fragmentos muy cortos moviendo frases desde el bloque anterior.</summary>
    private static List<string> Rebalance(List<string> chunks)
    {
        for (var i = 1; i < chunks.Count; i++)
        {
            while (ParagraphText.CountWords(chunks[i]) < MinChunkWords)
            {
                var previousSentences = ParagraphText.SplitSentences(chunks[i - 1]);
                if (previousSentences.Count <= 1) break;

                var moved = previousSentences[^1];
                var remaining = string.Join(' ', previousSentences.Take(previousSentences.Count - 1));

                if (ParagraphText.CountWords(remaining) < MinChunkWords) break;

                chunks[i - 1] = remaining;
                chunks[i] = $"{moved} {chunks[i]}";
            }
        }

        return chunks;
    }
}
