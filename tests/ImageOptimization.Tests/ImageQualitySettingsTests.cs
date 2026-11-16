using Aplication.DTOs;
using Domine.Entities;

namespace ImageOptimization.Tests;

public class ImageQualitySettingsTests
{
    [Fact]
    public void Defaults_UseQuality80AndNoOverrides()
    {
        var settings = new ImageQualitySettings();

        Assert.Equal(80, settings.Quality);
        Assert.Null(settings.QualitySmall);
        Assert.Null(settings.QualityMedium);
        Assert.Null(settings.QualityLarge);
        Assert.Equal(80, settings.ForWidth(400));
        Assert.Equal(80, settings.ForWidth(800));
        Assert.Equal(80, settings.ForWidth(1600));
    }

    [Theory]
    [InlineData(70, null, null, null, 400, 70)]
    [InlineData(70, 50, null, null, 400, 50)]
    [InlineData(70, 50, null, null, 800, 70)]
    [InlineData(70, 50, 60, null, 800, 60)]
    [InlineData(70, 50, 60, 90, 1600, 90)]
    public void ForWidth_ResolvesOverrides(
        int global,
        int? small,
        int? medium,
        int? large,
        int width,
        int expected)
    {
        var settings = new ImageQualitySettings
        {
            Quality = global,
            QualitySmall = small,
            QualityMedium = medium,
            QualityLarge = large
        };

        Assert.Equal(expected, settings.ForWidth(width));
    }

    [Fact]
    public void Dto_ForWidth_UsesSameRules()
    {
        var dto = new ImageQualitySettingsDto
        {
            Quality = 75,
            QualitySmall = 55,
            QualityLarge = 95
        };

        Assert.Equal(55, dto.ForWidth(400));
        Assert.Equal(75, dto.ForWidth(800));
        Assert.Equal(95, dto.ForWidth(1600));
    }
}
