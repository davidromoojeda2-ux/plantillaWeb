using System.Text.RegularExpressions;
using Aplication.Interfaces;
using Domine.Entities;
using Infrastructure.Persistence;
using MongoDB.Bson;
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

    public async Task<List<ImageMetadata>> GetByIdsAsync(IEnumerable<string> ids)
    {
        var idList = ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (idList.Count == 0) return [];

        return await _col.Find(i => idList.Contains(i.Id)).ToListAsync();
    }

    public async Task<List<ImageMetadata>> GetByArticleIdAsync(string articleId) =>
        await _col.Find(i => i.ArticleId == articleId).SortByDescending(i => i.UploadedAt).ToListAsync();

    public async Task<ImageMetadata?> GetByIdAsync(string id) =>
        await _col.Find(i => i.Id == id).FirstOrDefaultAsync();

    public async Task<ImageMetadata?> GetByNameAsync(string fileName, CancellationToken ct = default)
    {
        var trimmed = fileName.Trim();
        var regex = new BsonRegularExpression($"^{Regex.Escape(trimmed)}$", "i");
        var filter = Builders<ImageMetadata>.Filter.Regex(i => i.OriginalFileName, regex);
        return await _col.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string fileName, CancellationToken ct = default)
    {
        var trimmed = fileName.Trim();
        var regex = new BsonRegularExpression($"^{Regex.Escape(trimmed)}$", "i");
        var filter = Builders<ImageMetadata>.Filter.Regex(i => i.OriginalFileName, regex);
        return await _col.Find(filter).AnyAsync(ct);
    }

    public async Task<long> CountAsync(string? search = null, string? storage = null, string? articleId = null, CancellationToken ct = default)
    {
        var filter = BuildFilter(search, storage, articleId);
        return await _col.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    public async Task<List<ImageMetadata>> GetPagedAsync(int skip, int take, string? search = null, string? storage = null, string? articleId = null, CancellationToken ct = default)
    {
        var filter = BuildFilter(search, storage, articleId);
        return await _col.Find(filter)
            .SortByDescending(i => i.UploadedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(ct);
    }

    public async Task InsertAsync(ImageMetadata image) => await _col.InsertOneAsync(image);

    public async Task UpdateAltAsync(string id, string alt) =>
        await _col.UpdateOneAsync(i => i.Id == id,
            Builders<ImageMetadata>.Update.Set(i => i.Alt, alt));

    public async Task DeleteAsync(string id) => await _col.DeleteOneAsync(i => i.Id == id);

    private static FilterDefinition<ImageMetadata> BuildFilter(string? search, string? storage, string? articleId)
    {
        var builder = Builders<ImageMetadata>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var clean = Regex.Escape(search.Trim());
            var regex = new BsonRegularExpression(clean, "i");
            filter &= (builder.Regex(i => i.OriginalFileName, regex) | builder.Regex(i => i.Alt, regex));
        }

        if (!string.IsNullOrWhiteSpace(storage))
        {
            var st = storage.Trim().ToLowerInvariant();
            if (st == "s3" || st == "r2")
            {
                filter &= (builder.Eq(i => i.Storage, "s3") | builder.Eq(i => i.Storage, "r2"));
            }
            else
            {
                filter &= builder.Eq(i => i.Storage, st);
            }
        }

        if (!string.IsNullOrWhiteSpace(articleId))
        {
            if (articleId.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                filter &= (builder.Eq(i => i.ArticleId, null) | builder.Eq(i => i.ArticleId, ""));
            }
            else if (articleId.Equals("linked", StringComparison.OrdinalIgnoreCase))
            {
                filter &= (builder.Ne(i => i.ArticleId, null) & builder.Ne(i => i.ArticleId, ""));
            }
            else
            {
                filter &= builder.Eq(i => i.ArticleId, articleId);
            }
        }

        return filter;
    }
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
