using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>
/// R1: longitud acotada. Superar el máximo recomendado es suave; superar el máximo
/// absoluto (1,5×) es duro. Quedarse por debajo del mínimo también es suave.
/// </summary>
internal sealed class ParagraphLengthRule : IParagraphRule
{
    public IEnumerable<ParagraphFinding> Evaluate(ParagraphContext context)
    {
        var profile = context.Profile;
        var paragraph = 0;

        for (var i = 0; i < context.Blocks.Count; i++)
        {
            if (context.Blocks[i] is not ContentParagraphBlockDto block) continue;
            paragraph++;

            var words = ParagraphText.CountWords(block.Text);
            if (words == 0) continue;

            if (words > profile.HardMaxWords)
            {
                yield return new ParagraphFinding(
                    "length",
                    ParagraphSeverity.Hard,
                    $"El párrafo {paragraph} tiene {words} palabras; el máximo absoluto es {profile.HardMaxWords} (recomendado: hasta {profile.MaxWords}). Divídelo en dos.",
                    i,
                    words);
            }
            else if (words > profile.MaxWords)
            {
                yield return new ParagraphFinding(
                    "length",
                    ParagraphSeverity.Soft,
                    $"El párrafo {paragraph} tiene {words} palabras y supera el máximo recomendado de {profile.MaxWords}.",
                    i,
                    words);
            }
            else if (words < profile.MinWords)
            {
                yield return new ParagraphFinding(
                    "length",
                    ParagraphSeverity.Soft,
                    $"El párrafo {paragraph} tiene {words} palabras y no alcanza el mínimo recomendado de {profile.MinWords}.",
                    i,
                    words);
            }
        }
    }
}
