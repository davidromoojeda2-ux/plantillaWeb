using Aplication.Interfaces;
using Domine.Entities;
using Infrastructure.Persistence;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IMongoCollection<Category> _col;

    public CategoryRepository(MongoDbContext ctx) => _col = ctx.Categories;

    public async Task<List<Category>> GetAllAsync() =>
        await _col.Find(_ => true).SortBy(c => c.Order).ToListAsync();

    public async Task<List<Category>> GetMenuCategoriesAsync() =>
        await _col.Find(c => c.IsActive && c.ShowInMenu).SortBy(c => c.Order).ToListAsync();

    public async Task<Category?> GetByIdAsync(string id) =>
        await _col.Find(c => c.Id == id).FirstOrDefaultAsync();

    public async Task<Category?> GetBySlugAsync(string slug) =>
        await _col.Find(c => c.Slug == slug).FirstOrDefaultAsync();

    public async Task<List<Category>> GetByParentIdAsync(string? parentId) =>
        await _col.Find(c => c.ParentId == parentId).SortBy(c => c.Order).ToListAsync();

    public async Task InsertAsync(Category category) =>
        await _col.InsertOneAsync(category);

    public async Task UpdateAsync(Category category)
    {
        category.UpdatedAt = DateTime.UtcNow;
        await _col.ReplaceOneAsync(c => c.Id == category.Id, category);
    }

    public async Task DeleteAsync(string id) =>
        await _col.DeleteOneAsync(c => c.Id == id);

    public async Task<bool> SlugExistsAsync(string slug, string? excludeId = null)
    {
        var filter = Builders<Category>.Filter.Eq(c => c.Slug, slug);
        if (excludeId is not null)
            filter &= Builders<Category>.Filter.Ne(c => c.Id, excludeId);
        return await _col.CountDocumentsAsync(filter) > 0;
    }
}
