using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;
using Aplication.Services;

namespace ArticleAutomation.Tests;

public class GeneratedArticleValidatorTests
{
    [Fact]
    public void Validate_Returns_Valid_For_Well_Formed_Article()
    {
        var result = GeneratedArticleValidator.Validate(BuildArticle(), Plan());

        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
    }

    [Fact]
    public void Validate_Reports_Word_Count_Below_Tolerance()
    {
        var article = BuildArticle(wordCount: 20);

        var result = GeneratedArticleValidator.Validate(article, Plan());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("palabras", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Reports_Missing_Required_List()
    {
        var article = BuildArticle();
        article.Blocks.RemoveAll(b => b is ContentBulletListBlockDto or ContentOrderedListBlockDto);

        var result = GeneratedArticleValidator.Validate(article, Plan());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("lista", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Reports_Heading_As_First_Block()
    {
        var article = BuildArticle();
        article.Blocks.Insert(0, new ContentHeadingBlockDto { Level = 2, Text = "Introducción" });

        var result = GeneratedArticleValidator.Validate(article, Plan());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("empezar con un encabezado", StringComparison.OrdinalIgnoreCase));
    }

    private static ArticleGenerationPlan Plan() => new(
        TargetWords: 1000,
        MaxTokens: 5000,
        MinBlocks: 5,
        WordTolerancePercent: 20,
        MaxGenerationAttempts: 2,
        Template: "news",
        Tone: "informativo",
        Temperature: 0.5,
        IncludeList: true,
        IncludeTable: false,
        IncludeQuote: false,
        IncludeConclusion: true,
        IncludeFaq: false,
        IncludeCta: false,
        IncludeLinks: false);

    private static GeneratedArticle BuildArticle(int wordCount = 820)
    {
        var filler = string.Join(' ', Enumerable.Repeat("contenido", wordCount));

        return new GeneratedArticle
        {
            Title = "Título de prueba",
            Summary = "Resumen de prueba del artículo generado.",
            MetaDescription = "Meta descripción de prueba con la longitud necesaria para validar el artículo generado.",
            Tags = ["uno", "dos", "tres"],
            Blocks =
            [
                new ContentParagraphBlockDto { Text = $"Introducción del artículo. {filler}" },
                new ContentHeadingBlockDto { Level = 2, Text = "Desarrollo" },
                new ContentParagraphBlockDto { Text = "Desarrollo del tema con detalle y contexto." },
                new ContentBulletListBlockDto { Items = ["Primer punto", "Segundo punto", "Tercer punto"] },
                new ContentHeadingBlockDto { Level = 2, Text = "Conclusión" },
                new ContentParagraphBlockDto { Text = "Cierre del artículo con una idea final." }
            ]
        };
    }
}
