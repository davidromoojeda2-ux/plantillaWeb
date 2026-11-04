using System.Text.RegularExpressions;
using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>Resultado de validar un artículo generado contra la configuración editorial.</summary>
public sealed record ArticleValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public string ToCorrectionHint() =>
        string.Join("\n", Errors.Select(e => $"- {e}"));
}

/// <summary>
/// Valida la estructura de un artículo generado por IA (extensión, bloques, jerarquía de
/// encabezados, secciones requeridas y metadatos) para pedir una corrección si no cumple.
/// </summary>
public static class GeneratedArticleValidator
{
    private static readonly Regex WordRegex = new(@"\S+", RegexOptions.Compiled);
    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);

    public static ArticleValidationResult Validate(GeneratedArticle article, ArticleGenerationPlan plan)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(article.Title))
            errors.Add("Falta el título del artículo.");

        if (string.IsNullOrWhiteSpace(article.Summary))
            errors.Add("Falta el resumen (\"summary\").");
        else if (article.Summary.Trim().Length > 320)
            errors.Add("El resumen supera los 320 caracteres; hazlo más breve.");

        if (string.IsNullOrWhiteSpace(article.MetaDescription))
            errors.Add("Falta la meta descripción (\"metaDescription\").");
        else if (article.MetaDescription.Trim().Length > 320)
            errors.Add("La meta descripción supera los 320 caracteres; redúcela.");

        if (article.Tags.Count < 3)
            errors.Add("Incluye al menos 3 etiquetas (\"tags\").");

        var blocks = article.Blocks ?? [];
        if (blocks.Count == 0)
        {
            errors.Add("El artículo no contiene bloques de contenido.");
            return new ArticleValidationResult(false, errors);
        }

        var minBlocks = Math.Max(3, plan.MinBlocks);
        if (blocks.Count < minBlocks)
            errors.Add($"El artículo tiene {blocks.Count} bloques y el mínimo configurado es {minBlocks}.");

        ValidateWordCount(article, plan, errors);
        ValidateHierarchy(blocks, errors);
        ValidateRequiredSections(blocks, plan, errors);

        return new ArticleValidationResult(errors.Count == 0, errors);
    }

    private static void ValidateWordCount(GeneratedArticle article, ArticleGenerationPlan plan, List<string> errors)
    {
        if (plan.TargetWords <= 0) return;

        var words = CountWords(article);
        var tolerance = Math.Clamp(plan.WordTolerancePercent, 0, 80) / 100.0;

        // Se flexibiliza el mínimo: además del margen porcentual se toleran hasta 100
        // palabras (o un 15 % del objetivo) por debajo para no rechazar artículos válidos.
        var slack = Math.Min(100, (int)(plan.TargetWords * 0.15));
        var min = Math.Max(0, (int)Math.Floor(plan.TargetWords * (1 - tolerance)) - slack);
        var max = (int)Math.Ceiling(plan.TargetWords * (1 + tolerance));

        if (words < min)
            errors.Add($"El texto tiene {words} palabras y el mínimo aceptado es {min} (objetivo {plan.TargetWords}). Amplía el contenido con párrafos desarrollados.");
        else if (words > max)
            errors.Add($"El texto tiene {words} palabras y el máximo aceptado es {max} (objetivo {plan.TargetWords}). Recorta sin perder lo esencial.");
    }

    private static void ValidateHierarchy(IReadOnlyList<ContentBlockDto> blocks, List<string> errors)
    {
        if (blocks[0] is ContentHeadingBlockDto)
            errors.Add("El artículo no debe empezar con un encabezado; comienza con un párrafo introductorio.");

        if (blocks.Any(b => b is ContentHeadingBlockDto { Level: 1 }))
            errors.Add("No uses encabezados de nivel 1; el título va en \"title\" y las secciones usan nivel 2 o 3.");

        for (var i = 1; i < blocks.Count; i++)
        {
            // Un H2 seguido de un H3 es una jerarquía válida; solo es un problema
            // cuando se encadenan dos encabezados del mismo nivel sin contenido.
            if (blocks[i - 1] is ContentHeadingBlockDto previous &&
                blocks[i] is ContentHeadingBlockDto current &&
                previous.Level == current.Level)
            {
                errors.Add("Hay dos encabezados del mismo nivel consecutivos; cada sección debe ir seguida de contenido.");
                break;
            }
        }

        if (blocks.Any(IsEmptyBlock))
            errors.Add("Hay bloques vacíos o listas con menos de 2 elementos; completa o elimina esos bloques.");
    }

    private static void ValidateRequiredSections(
        IReadOnlyList<ContentBlockDto> blocks, ArticleGenerationPlan plan, List<string> errors)
    {
        var hasList = blocks.Any(b => b is ContentBulletListBlockDto or ContentOrderedListBlockDto);
        var hasTable = blocks.Any(b => b is ContentTableBlockDto);
        var hasQuote = blocks.Any(b => b is ContentQuoteBlockDto);
        var hasFaq = blocks.Any(b => b is ContentHeadingBlockDto h && IsFaqHeading(h.Text));
        var hasCta = blocks.Any(b => b is ContentCalloutBlockDto);
        var hasLinks = blocks.Any(ContainsAnchor);

        if (plan.IncludeList && !hasList)
            errors.Add("Falta al menos una lista con viñetas o numerada.");

        if (plan.IncludeTable && !hasTable)
            errors.Add("Falta la tabla requerida con encabezados y al menos 2 filas.");

        if (plan.IncludeQuote && !hasQuote)
            errors.Add("Falta la cita requerida (\"quote\") con atribución.");

        if (plan.IncludeFaq && !hasFaq)
            errors.Add("Falta la sección de preguntas frecuentes (encabezado \"Preguntas frecuentes\").");

        if (plan.IncludeCta && !hasCta)
            errors.Add("Falta el callout de llamada a la acción (\"callout\").");

        if (plan.IncludeLinks && !hasLinks)
            errors.Add("Faltan enlaces de referencia; incluye etiquetas <a href=\"...\"> internas o externas.");

        if (plan.IncludeConclusion)
        {
            var conclusionIndex = blocks
                .Select((b, i) => (b, i))
                .Where(x => x.b is ContentHeadingBlockDto h && IsConclusionHeading(h.Text))
                .Select(x => x.i)
                .DefaultIfEmpty(-1)
                .Last();

            var endsWithParagraph = blocks[^1] is ContentParagraphBlockDto;

            if (conclusionIndex >= 0)
            {
                var hasParagraphAfter = blocks.Skip(conclusionIndex + 1).Any(b => b is ContentParagraphBlockDto);
                if (!hasParagraphAfter)
                    errors.Add("La sección de conclusión debe cerrar con al menos un párrafo.");
            }
            else if (!endsWithParagraph)
            {
                errors.Add("Cierra el artículo con un párrafo de conclusión (puedes precederlo de un encabezado \"Conclusión\").");
            }
        }
    }

    private static bool IsConclusionHeading(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = text.Trim().ToLowerInvariant();
        return normalized.Contains("conclu") || normalized.Contains("cierre") || normalized.Contains("para terminar");
    }

    private static bool IsFaqHeading(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = text.Trim().ToLowerInvariant();
        return normalized.Contains("pregunta") || normalized.Contains("faq") || normalized.Contains("frecuente");
    }

    private static bool ContainsAnchor(ContentBlockDto block) => block switch
    {
        ContentParagraphBlockDto p => HasAnchor(p.Text),
        ContentBulletListBlockDto l => l.Items.Any(HasAnchor),
        ContentOrderedListBlockDto l => l.Items.Any(HasAnchor),
        ContentQuoteBlockDto q => HasAnchor(q.Text),
        ContentCalloutBlockDto c => HasAnchor(c.Text),
        _ => false
    };

    private static bool HasAnchor(string? text) =>
        !string.IsNullOrWhiteSpace(text)
        && text.Contains("href", StringComparison.OrdinalIgnoreCase)
        && text.Contains("<a", StringComparison.OrdinalIgnoreCase);

    private static bool IsEmptyBlock(ContentBlockDto block) => block switch
    {
        ContentParagraphBlockDto p => string.IsNullOrWhiteSpace(p.Text),
        ContentHeadingBlockDto h => string.IsNullOrWhiteSpace(h.Text),
        ContentQuoteBlockDto q => string.IsNullOrWhiteSpace(q.Text),
        ContentCalloutBlockDto c => string.IsNullOrWhiteSpace(c.Text),
        ContentBulletListBlockDto l => l.Items.Count < 2 || l.Items.All(string.IsNullOrWhiteSpace),
        ContentOrderedListBlockDto l => l.Items.Count < 2 || l.Items.All(string.IsNullOrWhiteSpace),
        ContentTableBlockDto t => t.Headers.Count == 0 || t.Rows.Count < 2,
        ContentCodeBlockDto c => string.IsNullOrWhiteSpace(c.Code),
        _ => false
    };

    private static int CountWords(GeneratedArticle article)
    {
        var texts = new List<string> { article.Title, article.Summary };

        foreach (var block in article.Blocks ?? [])
        {
            switch (block)
            {
                case ContentParagraphBlockDto p: texts.Add(p.Text); break;
                case ContentHeadingBlockDto h: texts.Add(h.Text); break;
                case ContentQuoteBlockDto q: texts.Add(q.Text); break;
                case ContentCalloutBlockDto c: texts.Add($"{c.Title} {c.Text}"); break;
                case ContentBulletListBlockDto l: texts.AddRange(l.Items); break;
                case ContentOrderedListBlockDto l: texts.AddRange(l.Items); break;
                case ContentTableBlockDto t:
                    texts.AddRange(t.Headers);
                    texts.AddRange(t.Rows.SelectMany(r => r));
                    break;
            }
        }

        var plain = TagRegex.Replace(string.Join(' ', texts), " ");
        return WordRegex.Matches(plain).Count;
    }
}
