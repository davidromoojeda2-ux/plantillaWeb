using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;
using Aplication.Services;

namespace ArticleAutomation.Tests;

public class GeneratedArticleSectionFilterTests
{
    [Fact]
    public void Apply_Removes_Unsolicited_Conclusion_Section()
    {
        var article = BuildArticle(
            new ContentHeadingBlockDto { Level = 2, Text = "Conclusión" },
            new ContentParagraphBlockDto { Text = "Cierre del artículo." });

        GeneratedArticleSectionFilter.Apply(article, Plan());

        Assert.DoesNotContain(article.Blocks, b =>
            b is ContentHeadingBlockDto h && h.Text.Contains("Conclusión"));
        Assert.DoesNotContain(article.Blocks, b =>
            b is ContentParagraphBlockDto p && p.Text.Contains("Cierre"));
    }

    [Fact]
    public void Apply_Keeps_Conclusion_When_Selected()
    {
        var article = BuildArticle(
            new ContentHeadingBlockDto { Level = 2, Text = "Conclusión" },
            new ContentParagraphBlockDto { Text = "Cierre del artículo." });

        GeneratedArticleSectionFilter.Apply(article, Plan(includeConclusion: true));

        Assert.Contains(article.Blocks, b =>
            b is ContentHeadingBlockDto h && h.Text.Contains("Conclusión"));
    }

    [Fact]
    public void Apply_Removes_Unsolicited_Faq_And_Preserves_Next_Section()
    {
        var article = BuildArticle(
            new ContentHeadingBlockDto { Level = 2, Text = "Preguntas frecuentes" },
            new ContentHeadingBlockDto { Level = 3, Text = "¿Pregunta de ejemplo?" },
            new ContentParagraphBlockDto { Text = "Respuesta de ejemplo." },
            new ContentHeadingBlockDto { Level = 2, Text = "Otra sección" },
            new ContentParagraphBlockDto { Text = "Contenido posterior." });

        GeneratedArticleSectionFilter.Apply(article, Plan());

        Assert.DoesNotContain(article.Blocks, b =>
            b is ContentHeadingBlockDto h && h.Text.Contains("Preguntas frecuentes"));
        Assert.DoesNotContain(article.Blocks, b =>
            b is ContentParagraphBlockDto p && p.Text.Contains("Respuesta de ejemplo"));
        Assert.Contains(article.Blocks, b =>
            b is ContentHeadingBlockDto h && h.Text == "Otra sección");
        Assert.Contains(article.Blocks, b =>
            b is ContentParagraphBlockDto p && p.Text == "Contenido posterior.");
    }

    [Fact]
    public void Apply_Keeps_Faq_When_Selected()
    {
        var article = BuildArticle(
            new ContentHeadingBlockDto { Level = 2, Text = "Preguntas frecuentes" },
            new ContentHeadingBlockDto { Level = 3, Text = "¿Pregunta?" },
            new ContentParagraphBlockDto { Text = "Respuesta." });

        GeneratedArticleSectionFilter.Apply(article, Plan(includeFaq: true));

        Assert.Contains(article.Blocks, b =>
            b is ContentHeadingBlockDto h && h.Text.Contains("Preguntas frecuentes"));
    }

    [Fact]
    public void Apply_Keeps_Faq_For_Faq_Template_Even_If_Flag_Is_False()
    {
        var article = BuildArticle(
            new ContentHeadingBlockDto { Level = 2, Text = "Preguntas frecuentes" },
            new ContentHeadingBlockDto { Level = 3, Text = "¿Pregunta?" },
            new ContentParagraphBlockDto { Text = "Respuesta." });

        GeneratedArticleSectionFilter.Apply(article, Plan(template: "faq", includeFaq: false));

        Assert.Contains(article.Blocks, b =>
            b is ContentHeadingBlockDto h && h.Text.Contains("Preguntas frecuentes"));
    }

    private static GeneratedArticle BuildArticle(params ContentBlockDto[] extraBlocks)
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto { Text = "Introducción." },
            new ContentHeadingBlockDto { Level = 2, Text = "Desarrollo" },
            new ContentParagraphBlockDto { Text = "Contenido de desarrollo." }
        };
        blocks.AddRange(extraBlocks);

        return new GeneratedArticle
        {
            Title = "Artículo de prueba",
            Summary = "Resumen de prueba.",
            MetaDescription = "Meta descripción de prueba.",
            Tags = ["uno", "dos", "tres"],
            Blocks = blocks
        };
    }

    private static ArticleGenerationPlan Plan(
        string template = "informative",
        bool includeConclusion = false,
        bool includeFaq = false) => new(
            TargetWords: 1000,
            MaxTokens: 4000,
            MinBlocks: 3,
            WordTolerancePercent: 20,
            MaxGenerationAttempts: 1,
            Template: template,
            Tone: "informativo",
            Temperature: 0.5,
            IncludeList: false,
            IncludeTable: false,
            IncludeQuote: false,
            IncludeConclusion: includeConclusion,
            IncludeFaq: includeFaq,
            IncludeCta: false,
            IncludeLinks: false);
}
