using Aplication.Interfaces;
using Domine.Entities;
using Infrastructure.Persistence;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

public class InformativePageRepository : IInformativePageRepository
{
    private readonly IMongoCollection<InformativePage> _col;

    public InformativePageRepository(MongoDbContext ctx) => _col = ctx.Pages;

    public async Task<List<InformativePage>> GetAllAsync() =>
        await _col.Find(_ => true).SortBy(p => p.Order).ToListAsync();

    public async Task<List<InformativePage>> GetFooterPagesAsync() =>
        await _col.Find(p => p.ShowInFooter).SortBy(p => p.Order).ToListAsync();

    public async Task<InformativePage?> GetBySlugAsync(string slug) =>
        await _col.Find(p => p.Slug == slug).FirstOrDefaultAsync();

    public async Task<InformativePage?> GetByIdAsync(string id) =>
        await _col.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task InsertAsync(InformativePage page) => await _col.InsertOneAsync(page);

    public async Task UpdateAsync(InformativePage page)
    {
        page.UpdatedAt = DateTime.UtcNow;
        await _col.ReplaceOneAsync(p => p.Id == page.Id, page);
    }

    public async Task DeleteAsync(string id) => await _col.DeleteOneAsync(p => p.Id == id);

    public async Task<bool> SlugExistsAsync(string slug, string? excludeId = null)
    {
        var filter = Builders<InformativePage>.Filter.Eq(p => p.Slug, slug);
        if (excludeId is not null)
            filter &= Builders<InformativePage>.Filter.Ne(p => p.Id, excludeId);
        return await _col.CountDocumentsAsync(filter) > 0;
    }
}

public class ImageMetadataRepository : IImageMetadataRepository
{
    private readonly IMongoCollection<ImageMetadata> _col;

    public ImageMetadataRepository(MongoDbContext ctx) => _col = ctx.Images;

    public async Task<List<ImageMetadata>> GetAllAsync(int skip = 0, int take = 50) =>
        await _col.Find(_ => true).SortByDescending(i => i.UploadedAt).Skip(skip).Limit(take).ToListAsync();

    public async Task<List<ImageMetadata>> GetByArticleIdAsync(string articleId) =>
        await _col.Find(i => i.ArticleId == articleId).SortByDescending(i => i.UploadedAt).ToListAsync();

    public async Task<ImageMetadata?> GetByIdAsync(string id) =>
        await _col.Find(i => i.Id == id).FirstOrDefaultAsync();

    public async Task InsertAsync(ImageMetadata image) => await _col.InsertOneAsync(image);

    public async Task UpdateAltAsync(string id, string alt) =>
        await _col.UpdateOneAsync(i => i.Id == id,
            Builders<ImageMetadata>.Update.Set(i => i.Alt, alt));

    public async Task UpdateArticleIdAsync(string id, string? articleId) =>
        await _col.UpdateOneAsync(i => i.Id == id,
            Builders<ImageMetadata>.Update.Set(i => i.ArticleId, string.IsNullOrWhiteSpace(articleId) ? null : articleId));

    public async Task DeleteAsync(string id) => await _col.DeleteOneAsync(i => i.Id == id);
}

public class AdminUserRepository : IAdminUserRepository
{
    private readonly IMongoCollection<AdminUser> _col;

    public AdminUserRepository(MongoDbContext ctx) => _col = ctx.Users;

    public async Task<AdminUser?> GetByUsernameAsync(string username) =>
        await _col.Find(u => u.Username == username && u.IsActive).FirstOrDefaultAsync();

    public async Task<AdminUser?> GetByIdAsync(string id) =>
        await _col.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task InsertAsync(AdminUser user) => await _col.InsertOneAsync(user);

    public async Task UpdateLastLoginAsync(string id) =>
        await _col.UpdateOneAsync(u => u.Id == id,
            Builders<AdminUser>.Update.Set(u => u.LastLoginAt, DateTime.UtcNow));
}
