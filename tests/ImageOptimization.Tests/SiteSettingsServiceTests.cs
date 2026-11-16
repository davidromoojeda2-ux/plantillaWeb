using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace ImageOptimization.Tests;

public class SiteSettingsServiceTests
{
    [Fact]
    public async Task SaveImageQuality_PersistsAndInvalidatesCache()
    {
        var (service, repository, cache) = CreateService();

        var initial = await service.GetImageQualityAsync();
        Assert.Equal(80, initial.Quality);

        await service.SaveImageQualityAsync(new ImageQualitySettingsDto
        {
            Quality = 72,
            QualitySmall = 60,
            QualityLarge = 88
        });

        var updated = await service.GetImageQualityAsync();

        Assert.Equal(72, updated.Quality);
        Assert.Equal(60, updated.QualitySmall);
        Assert.Null(updated.QualityMedium);
        Assert.Equal(88, updated.QualityLarge);
        Assert.Equal(72, repository.Settings.ImageQuality.Quality);

        cache.Dispose();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task SaveImageQuality_RejectsOutOfRangeValues(int quality)
    {
        var (service, _, cache) = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveImageQualityAsync(new ImageQualitySettingsDto { Quality = quality }));

        Assert.Contains("entre 1 y 100", exception.Message);
        cache.Dispose();
    }

    [Fact]
    public async Task SaveImageQuality_RejectsOutOfRangeOverride()
    {
        var (service, _, cache) = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveImageQualityAsync(new ImageQualitySettingsDto
            {
                Quality = 80,
                QualitySmall = 120
            }));

        Assert.Contains("400 px", exception.Message);
        cache.Dispose();
    }

    private static (SiteSettingsService Service, FakeSiteSettingsRepository Repository, MemoryCache Cache) CreateService()
    {
        var repository = new FakeSiteSettingsRepository();
        var cache = new MemoryCache(new MemoryCacheOptions());
        return (new SiteSettingsService(repository, cache), repository, cache);
    }

    private sealed class FakeSiteSettingsRepository : ISiteSettingsRepository
    {
        public SiteSettings Settings { get; private set; } = new();

        public Task<SiteSettings> GetAsync() => Task.FromResult(Settings);

        public Task SaveAsync(SiteSettings settings)
        {
            Settings = settings;
            return Task.CompletedTask;
        }
    }
}
