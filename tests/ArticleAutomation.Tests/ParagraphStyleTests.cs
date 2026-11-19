using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;
using Aplication.Services;
using Domine.Entities;

namespace ArticleAutomation.Tests;

public class ParagraphStyleTests
{
    [Fact]
    public void Profile_Uses_Template_Defaults()
    {
        var news = ParagraphStyleProfile.Resolve("news", null, "es");
        Assert.Equal(40, news.MinWords);
        Assert.Equal(70, news.MaxWords);
        Assert.Equal(3, news.MaxConsecutive);
        Assert.Equal(105, news.HardMaxWords);

        var faq = ParagraphStyleProfile.Resolve("faq", null, "es");
        Assert.Equal(80, faq.MaxWords);
        Assert.Equal(1, faq.MaxConsecutive);

        var unknown = ParagraphStyleProfile.Resolve("desconocida", null, "es");
        Assert.Equal(50, unknown.MinWords);
        Assert.Equal(110, unknown.MaxWords);
    }

    [Fact]
    public void Profile_Applies_Configured_Overrides()
    {
        var overrides = new Dictionary<string, ParagraphStyleSetting>
        {
            ["news"] = new() { MinWords = 100, MaxWords = 150, MaxConsecutive = 4 }
        };

        var profile = ParagraphStyleProfile.Resolve("news", overrides, "es");

        Assert.Equal(100, profile.MinWords);
        Assert.Equal(150, profile.MaxWords);
        Assert.Equal(4, profile.MaxConsecutive);
        Assert.Equal(225, profile.HardMaxWords);
    }

    [Fact]
    public void Profile_Falls_Back_When_Override_Is_Out_Of_Range()
    {
        var overrides = new Dictionary<string, ParagraphStyleSetting>
        {
            ["news"] = new() { MinWords = 1, MaxWords = 10, MaxConsecutive = 0 }
        };

        var profile = ParagraphStyleProfile.Resolve("news", overrides, "es");

        Assert.Equal(40, profile.MinWords);
        Assert.Equal(70, profile.MaxWords);
        Assert.Equal(3, profile.MaxConsecutive);
    }

    [Fact]
    public void Length_Rule_Flags_Soft_And_Hard()
    {
        var soft = ParagraphStyle.Analyze(
            [Paragraph(Words(80))], ParagraphStyleProfile.Resolve("news", null, "es"));
        var softFinding = Assert.Single(soft, f => f.Code == "length");
        Assert.Equal(ParagraphSeverity.Soft, softFinding.Severity);

        var hard = ParagraphStyle.Analyze(
            [Paragraph(Words(130))], ParagraphStyleProfile.Resolve("news", null, "es"));
        var hardFinding = Assert.Single(hard, f => f.Code == "length");
        Assert.Equal(ParagraphSeverity.Hard, hardFinding.Severity);
    }

    [Fact]
    public void AnswerFirst_Rule_Flags_Filler_Opener()
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentHeadingBlockDto { Level = 2, Text = "Qué es" },
            Paragraph("En la actualidad, el tema ocupa un lugar central. La respuesta concreta es otra.")
        };

        var findings = ParagraphStyle.Analyze(blocks, ParagraphStyleProfile.Resolve("news", null, "es"));

        Assert.Contains(findings, f => f.Code == "opener");
    }

    [Fact]
    public void AnswerFirst_Rule_Flags_Long_First_Sentence()
    {
        var longSentence = string.Join(' ', Enumerable.Repeat("palabra", 30)) + ".";
        var blocks = new List<ContentBlockDto>
        {
            new ContentHeadingBlockDto { Level = 2, Text = "Detalle" },
            Paragraph(longSentence)
        };

        var findings = ParagraphStyle.Analyze(blocks, ParagraphStyleProfile.Resolve("news", null, "es"));

        Assert.Contains(findings, f => f.Code == "first-sentence");
    }

    [Fact]
    public void Stacking_Rule_Flags_Runs_Above_The_Limit()
    {
        var blocks = Enumerable.Range(0, 4)
            .Select(_ => (ContentBlockDto)Paragraph("Una idea breve del tema."))
            .ToList();

        var findings = ParagraphStyle.Analyze(blocks, ParagraphStyleProfile.Resolve("news", null, "es"));

        var stacked = Assert.Single(findings, f => f.Code == "stacked");
        Assert.Equal(4, stacked.Value);
        Assert.Equal(ParagraphSeverity.Soft, stacked.Severity);
    }

    [Fact]
    public void Stacking_Rule_Is_Hard_When_The_Run_Is_Much_Longer()
    {
        var blocks = Enumerable.Range(0, 6)
            .Select(_ => (ContentBlockDto)Paragraph("Una idea breve del tema."))
            .ToList();

        var findings = ParagraphStyle.Analyze(blocks, ParagraphStyleProfile.Resolve("news", null, "es"));

        var stacked = Assert.Single(findings, f => f.Code == "stacked");
        Assert.Equal(ParagraphSeverity.Hard, stacked.Severity);
    }

    [Fact]
    public void Normalizer_Splits_Paragraphs_Above_The_Maximum()
    {
        var sentence = string.Join(' ', Enumerable.Repeat("palabra", 9)) + ".";
        var text = string.Join(' ', Enumerable.Repeat(sentence, 25));
        var blocks = new List<ContentBlockDto> { Paragraph(text) };

        ParagraphNormalizer.Apply(blocks, ParagraphStyleProfile.Resolve("news", null, "es"));

        Assert.True(blocks.Count > 1);
        foreach (var paragraph in blocks.OfType<ContentParagraphBlockDto>())
        {
            var words = CountWords(paragraph.Text);
            Assert.InRange(words, 20, 70);
        }
    }

    [Fact]
    public void Normalizer_Does_Not_Touch_Paragraphs_With_Inline_Markup()
    {
        var text = "<a href=\"/articulos/fuente\">" + Words(200) + "</a>";
        var blocks = new List<ContentBlockDto> { Paragraph(text) };

        ParagraphNormalizer.Apply(blocks, ParagraphStyleProfile.Resolve("news", null, "es"));

        var only = Assert.Single(blocks);
        Assert.Equal(text, ((ContentParagraphBlockDto)only).Text);
    }

    [Fact]
    public void Pipeline_Removes_Forbidden_Sections_Before_Analyzing()
    {
        var article = Article(
            Paragraph("Texto inicial del artículo con una idea clara."),
            new ContentHeadingBlockDto { Level = 2, Text = "Conclusión" },
            Paragraph("Cierre que no fue solicitado."));

        ArticleDraftPipeline.Process(article, Plan(includeConclusion: false));

        Assert.DoesNotContain(article.Blocks, b => b is ContentHeadingBlockDto);
        Assert.Single(article.Blocks);
    }

    [Fact]
    public void Validator_Reports_Hard_Style_Without_Invalidating()
    {
        var article = Article(
            Paragraph(Words(200)),
            Paragraph("Segundo párrafo de apoyo con una idea clara."),
            Paragraph("Tercer párrafo de cierre del artículo."));

        var result = GeneratedArticleValidator.Validate(article, Plan());

        Assert.True(result.IsValid);
        Assert.True(result.HasHardStyleIssues);
        Assert.NotEmpty(result.EffectiveStyleHints);
    }

    [Fact]
    public void Prompt_Guidance_Comes_From_The_Same_Profile()
    {
        var profile = ParagraphStyleProfile.Resolve("news", null, "es");
        var guidance = ParagraphStyle.DescribeForPrompt(profile);

        Assert.Contains("entre 40 y 70", guidance);
        Assert.Contains("105", guidance);
        Assert.Contains("más de 3", guidance);
        Assert.Contains("en la actualidad", guidance);
    }

    [Fact]
    public void Openers_Are_Resolved_By_Language()
    {
        var english = ParagraphStyleProfile.Resolve("news", null, "en");
        Assert.Contains("when it comes to", english.ForbiddenOpeners);

        var unsupported = ParagraphStyleProfile.Resolve("news", null, "ru");
        Assert.Empty(unsupported.ForbiddenOpeners);
    }

    private static ContentParagraphBlockDto Paragraph(string text) => new() { Text = text };

    private static string Words(int count) => string.Join(' ', Enumerable.Repeat("palabra", count));

    private static int CountWords(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    private static GeneratedArticle Article(params ContentBlockDto[] blocks) => new()
    {
        Title = "Título de prueba",
        Summary = "Resumen de prueba del artículo.",
        MetaDescription = "Meta descripción de prueba.",
        Tags = ["uno", "dos", "tres"],
        Blocks = [.. blocks]
    };

    private static ArticleGenerationPlan Plan(bool includeConclusion = false) => new(
        TargetWords: 0,
        MaxTokens: 5000,
        MinBlocks: 1,
        WordTolerancePercent: 20,
        MaxGenerationAttempts: 1,
        Template: "news",
        Tone: "informativo",
        Temperature: 0.5,
        IncludeList: false,
        IncludeTable: false,
        IncludeQuote: false,
        IncludeConclusion: includeConclusion,
        IncludeFaq: false,
        IncludeCta: false,
        IncludeLinks: false);
}
