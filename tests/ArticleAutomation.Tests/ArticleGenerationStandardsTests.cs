using Aplication.DTOs.Automation;
using Domine.Entities;

namespace ArticleAutomation.Tests;

public class ArticleGenerationStandardsTests
{
    [Fact]
    public void ForWords_1500_Returns_Recommended_Standard()
    {
        var standard = ArticleGenerationStandards.ForWords(1500);

        Assert.Equal(10, standard.MinBlocks);
        Assert.Equal(15, standard.MaxBlocks);
        Assert.Equal(7250, standard.MaxTokens);
        Assert.Equal(25, standard.WordTolerancePercent);
    }

    [Fact]
    public void Planner_Uses_Auto_Standards_When_Enabled()
    {
        var plan = ArticleGenerationPlanner.FromSettings(new AutomationSettings
        {
            TargetWords = 1500,
            AutoStandards = true,
            MinBlocks = 5,
            MaxBlocks = 10,
            MaxTokensPerArticle = 0,
            WordTolerancePercent = 20
        });

        Assert.Equal(10, plan.MinBlocks);
        Assert.Equal(15, plan.MaxBlocks);
        Assert.Equal(7250, plan.MaxTokens);
        Assert.Equal(25, plan.WordTolerancePercent);
    }

    [Fact]
    public void Planner_Uses_Manual_Values_When_Auto_Disabled()
    {
        var plan = ArticleGenerationPlanner.FromSettings(new AutomationSettings
        {
            TargetWords = 1500,
            AutoStandards = false,
            MinBlocks = 5,
            MaxBlocks = 10,
            MaxTokensPerArticle = 0,
            WordTolerancePercent = 20
        });

        Assert.Equal(5, plan.MinBlocks);
        Assert.Equal(10, plan.MaxBlocks);
        Assert.Equal(6500, plan.MaxTokens);
        Assert.Equal(20, plan.WordTolerancePercent);
    }
}
