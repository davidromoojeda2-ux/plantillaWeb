using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>Detecta encabezados de secciones opcionales para validarlas o filtrarlas.</summary>
internal static class ArticleSectionClassifier
{
    public static bool IsConclusion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = text.Trim().ToLowerInvariant();
        return normalized.Contains("conclu") || normalized.Contains("cierre") || normalized.Contains("para terminar");
    }

    public static bool IsFaq(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = text.Trim().ToLowerInvariant();
        return normalized.Contains("pregunta") || normalized.Contains("faq") || normalized.Contains("frecuente");
    }
}

/// <summary>
/// Elimina las secciones opcionales que el usuario no seleccionó. La IA puede ignorar
/// la instrucción del prompt; este filtro garantiza que no se publiquen conclusiones
/// ni preguntas frecuentes no solicitadas.
/// </summary>
public static class GeneratedArticleSectionFilter
{
    public static void Apply(GeneratedArticle article, ArticleGenerationPlan plan)
    {
        if (article.Blocks is null || article.Blocks.Count == 0) return;

        if (!plan.IncludeConclusion)
            RemoveSections(article.Blocks, ArticleSectionClassifier.IsConclusion);

        var template = plan.Template?.Trim().ToLowerInvariant();
        if (!plan.IncludeFaq && template is not ("faq" or "ppa"))
            RemoveSections(article.Blocks, ArticleSectionClassifier.IsFaq);
    }

    /// <summary>
    /// Elimina desde el encabezado detectado hasta el siguiente encabezado del mismo
    /// nivel o superior, conservando el resto del artículo.
    /// </summary>
    private static void RemoveSections(List<ContentBlockDto> blocks, Func<string?, bool> isHeading)
    {
        for (var i = blocks.Count - 1; i >= 0; i--)
        {
            if (blocks[i] is not ContentHeadingBlockDto heading || !isHeading(heading.Text))
                continue;

            var end = blocks.Count;
            for (var j = i + 1; j < blocks.Count; j++)
            {
                if (blocks[j] is ContentHeadingBlockDto next && next.Level <= heading.Level)
                {
                    end = j;
                    break;
                }
            }

            blocks.RemoveRange(i, end - i);
        }
    }
}
