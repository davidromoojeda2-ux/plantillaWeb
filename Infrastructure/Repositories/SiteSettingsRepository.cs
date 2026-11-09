using Aplication.Interfaces;
using Domine.Entities;
using Infrastructure.Persistence;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

public class SiteSettingsRepository : ISiteSettingsRepository
{
    private readonly IMongoCollection<SiteSettings> _collection;

    public SiteSettingsRepository(MongoDbContext context)
    {
        _collection = context.SiteSettings;
    }

    public async Task<SiteSettings> GetAsync() =>
        await _collection.Find(s => s.Id == SiteSettings.SingletonId).FirstOrDefaultAsync()
        ?? new SiteSettings();

    public Task SaveAsync(SiteSettings settings)
    {
        settings.Id = SiteSettings.SingletonId;
        settings.UpdatedAt = DateTime.UtcNow;

        return _collection.ReplaceOneAsync(
            s => s.Id == SiteSettings.SingletonId,
            settings,
            new ReplaceOptions { IsUpsert = true });
    }
}
