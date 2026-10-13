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
        await _col.Find(a => a.Status == ArticleStatus.Publicado && a.CategoryId == categoryId)
                  .SortByDescending(a => a.PublishedAt).Skip(skip).Limit(take).ToListAsync();

    public async Task<long> CountByCategoryAsync(string categoryId) =>
        await _col.CountDocumentsAsync(a => a.Status == ArticleStatus.Publicado && a.CategoryId == categoryId);

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
}
