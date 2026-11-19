using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>
/// R2: respuesta inmediata. El primer párrafo tras cada encabezado (o el inicial del
/// artículo) debe responder en su primera frase, sin fórmulas de relleno.
/// </summary>
internal sealed class AnswerFirstRule : IParagraphRule
{
    public IEnumerable<ParagraphFinding> Evaluate(ParagraphContext context)
    {
        var profile = context.Profile;
        string? heading = null;
        var expectsFirstParagraph = true;

        for (var i = 0; i < context.Blocks.Count; i++)
        {
            var block = context.Blocks[i];

            if (block is ContentHeadingBlockDto current)
            {
                heading = ParagraphText.StripTags(current.Text).Trim();
                expectsFirstParagraph = true;
                continue;
            }

            if (block is not ContentParagraphBlockDto paragraph)
            {
                // Una lista, tabla, cita u otro bloque rompe la secuencia: el párrafo
                // posterior ya no es "el primero" de la sección.
                expectsFirstParagraph = false;
                continue;
            }

            if (!expectsFirstParagraph) continue;
            expectsFirstParagraph = false;

            var text = ParagraphText.StripTags(paragraph.Text).Trim();
            if (text.Length == 0) continue;

            var scope = string.IsNullOrWhiteSpace(heading)
                ? "el inicio del artículo"
                : $"la sección «{heading}»";

            var firstSentence = ParagraphText.FirstSentence(text);
            var words = ParagraphText.CountWords(firstSentence);

            if (words > profile.MaxFirstSentenceWords)
            {
                yield return new ParagraphFinding(
                    "first-sentence",
                    ParagraphSeverity.Soft,
                    $"La primera frase del párrafo que abre {scope} tiene {words} palabras; debe responder en {profile.MaxFirstSentenceWords} o menos.",
                    i,
                    words);
            }

            var normalized = ParagraphText.Normalize(firstSentence);
            var opener = profile.ForbiddenOpeners.FirstOrDefault(o =>
                normalized.StartsWith(ParagraphText.Normalize(o), StringComparison.Ordinal));

            if (opener is not null)
            {
                yield return new ParagraphFinding(
                    "opener",
                    ParagraphSeverity.Soft,
                    $"El párrafo que abre {scope} empieza con «{opener}», una fórmula de relleno. Debe comenzar por la respuesta.",
                    i);
            }
        }
    }
}
