using Aplication.Interfaces;
using Domine.Entities;
using Infrastructure.Persistence;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

public class ArticleRepository : IArticleRepository
{
    private readonly IMongoCollection<Article> _col;

    public ArticleRepository(MongoDbContext ctx) => _col = ctx.Articles;

    public async Task<List<Article>> GetPublishedAsync(int skip = 0, int take = 9) =>
        await _col.Find(a => a.Status == ArticleStatus.Publicado)
                  .SortByDescending(a => a.PublishedAt).Skip(skip).Limit(take).ToListAsync();

    public async Task<List<Article>> GetFeaturedAsync(int take = 3) =>
        await _col.Find(a => a.Status == ArticleStatus.Publicado && a.Featured)
                  .SortByDescending(a => a.PublishedAt).Limit(take).ToListAsync();

    public async Task<List<Article>> GetByCategoryAsync(string categoryId, int skip = 0, int take = 9) =>
        await _col.Find(a => a.Status == ArticleStatus.Publicado && (a.CategoryId == categoryId || a.SubcategoryId == categoryId))
                  .SortByDescending(a => a.PublishedAt).Skip(skip).Limit(take).ToListAsync();

    public async Task<long> CountByCategoryAsync(string categoryId) =>
        await _col.CountDocumentsAsync(a => a.Status == ArticleStatus.Publicado && (a.CategoryId == categoryId || a.SubcategoryId == categoryId));

    public async Task<long> CountPublishedAsync() =>
        await _col.CountDocumentsAsync(a => a.Status == ArticleStatus.Publicado);

    public async Task<Article?> GetBySlugAsync(string slug) =>
        await _col.Find(a => a.Slug == slug).FirstOrDefaultAsync();

    public async Task<Article?> GetByIdAsync(string id) =>
        await _col.Find(a => a.Id == id).FirstOrDefaultAsync();

    public async Task<List<Article>> GetAllAsync(int skip = 0, int take = 50) =>
        await _col.Find(_ => true).SortByDescending(a => a.UpdatedAt).Skip(skip).Limit(take).ToListAsync();

    public async Task<List<Article>> SearchAsync(string text, int skip = 0, int take = 9)
    {
        var filter = Builders<Article>.Filter.Text(text) &
                     Builders<Article>.Filter.Eq(a => a.Status, ArticleStatus.Publicado);
        return await _col.Find(filter)
                         .Sort(Builders<Article>.Sort.MetaTextScore("score"))
                         .Skip(skip).Limit(take).ToListAsync();
    }

    public async Task<long> CountSearchAsync(string text)
    {
        var filter = Builders<Article>.Filter.Text(text) &
                     Builders<Article>.Filter.Eq(a => a.Status, ArticleStatus.Publicado);
        return await _col.CountDocumentsAsync(filter);
    }

    public async Task InsertAsync(Article article) => await _col.InsertOneAsync(article);

    public async Task UpdateAsync(Article article)
    {
        article.UpdatedAt = DateTime.UtcNow;
        await _col.ReplaceOneAsync(a => a.Id == article.Id, article);
    }

    public async Task DeleteAsync(string id) => await _col.DeleteOneAsync(a => a.Id == id);

    public async Task IncrementViewCountAsync(string id) =>
        await _col.UpdateOneAsync(a => a.Id == id,
            Builders<Article>.Update.Inc(a => a.ViewCount, 1));

    public async Task<bool> SlugExistsAsync(string slug, string? excludeId = null)
    {
        var filter = Builders<Article>.Filter.Eq(a => a.Slug, slug);
        if (excludeId is not null)
            filter &= Builders<Article>.Filter.Ne(a => a.Id, excludeId);
        return await _col.CountDocumentsAsync(filter) > 0;
    }

    public async Task<List<Article>> GetRecommendedAsync(string currentArticleId, string? categoryId, int take = 3)
    {
        var result = new List<Article>();

        if (!string.IsNullOrEmpty(categoryId))
        {
            var categoryFilter = Builders<Article>.Filter.Eq(a => a.Status, ArticleStatus.Publicado) &
                                 Builders<Article>.Filter.Eq(a => a.CategoryId, categoryId) &
                                 Builders<Article>.Filter.Ne(a => a.Id, currentArticleId);

            var categoryItems = await _col.Find(categoryFilter)
                                          .SortByDescending(a => a.PublishedAt)
                                          .Limit(take)
                                          .ToListAsync();
            result.AddRange(categoryItems);
        }

        if (result.Count < take)
        {
            var needed = take - result.Count;
            var excludedIds = new List<string> { currentArticleId };
            excludedIds.AddRange(result.Select(r => r.Id));

            var fallbackFilter = Builders<Article>.Filter.Eq(a => a.Status, ArticleStatus.Publicado) &
                                 Builders<Article>.Filter.Nin(a => a.Id, excludedIds);

            var fallbackItems = await _col.Find(fallbackFilter)
                                          .SortByDescending(a => a.PublishedAt)
                                          .Limit(needed)
                                          .ToListAsync();
            result.AddRange(fallbackItems);
        }

        return result;
    }

    public async Task<HashSet<string>> GetCategoriesWithPublishedArticlesAsync()
    {
        var filter = Builders<Article>.Filter.Eq(a => a.Status, ArticleStatus.Publicado);
        var projection = Builders<Article>.Projection
            .Include(a => a.CategoryId)
            .Include(a => a.SubcategoryId);

        var list = await _col.Find(filter).Project(projection).ToListAsync();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var doc in list)
        {
            if (doc.TryGetValue("CategoryId", out var catVal) && !catVal.IsBsonNull)
            {
                var catId = catVal.AsString;
                if (!string.IsNullOrWhiteSpace(catId)) set.Add(catId);
            }
            if (doc.TryGetValue("SubcategoryId", out var subVal) && !subVal.IsBsonNull)
            {
                var subId = subVal.AsString;
                if (!string.IsNullOrWhiteSpace(subId)) set.Add(subId);
            }
        }

        return set;
    }
}
