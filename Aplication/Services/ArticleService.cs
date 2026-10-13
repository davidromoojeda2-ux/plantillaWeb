using Aplication.DTOs;
using Aplication.Interfaces;
using Domine.Entities;

namespace Aplication.Services;

public interface IArticleService
{
    Task<(List<Article> Items, long Total)> GetPublishedAsync(int page = 1, int pageSize = 9);
    Task<(List<Article> Items, long Total)> GetByCategoryAsync(string categoryId, int page = 1, int pageSize = 9);
    Task<(List<Article> Items, long Total)> SearchAsync(string query, int page = 1, int pageSize = 9);
    Task<List<Article>> GetFeaturedAsync(int take = 3);
    Task<Article?> GetBySlugAsync(string slug);
    Task<Article?> GetByIdAsync(string id);
    Task<List<Article>> GetAllAdminAsync(int page = 1, int pageSize = 50);
    Task<Article> CreateAsync(ArticleDto dto);
    Task<Article> UpdateAsync(string id, ArticleDto dto);
    Task DeleteAsync(string id);
    Task IncrementViewCountAsync(string id);
}

public class ArticleService : IArticleService
{
    private readonly IArticleRepository _repo;

    public ArticleService(IArticleRepository repo) => _repo = repo;

    public async Task<(List<Article>, long)> GetPublishedAsync(int page = 1, int pageSize = 9)
    {
        var skip = (page - 1) * pageSize;
        return (await _repo.GetPublishedAsync(skip, pageSize), await _repo.CountPublishedAsync());
    }

    public async Task<(List<Article>, long)> GetByCategoryAsync(string categoryId, int page = 1, int pageSize = 9)
    {
        var skip = (page - 1) * pageSize;
        return (await _repo.GetByCategoryAsync(categoryId, skip, pageSize), await _repo.CountByCategoryAsync(categoryId));
    }

    public async Task<(List<Article>, long)> SearchAsync(string query, int page = 1, int pageSize = 9)
    {
        var skip = (page - 1) * pageSize;
        return (await _repo.SearchAsync(query, skip, pageSize), await _repo.CountSearchAsync(query));
    }

    public Task<List<Article>> GetFeaturedAsync(int take = 3) => _repo.GetFeaturedAsync(take);
    public Task<Article?> GetBySlugAsync(string slug) => _repo.GetBySlugAsync(slug);
    public Task<Article?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);

    public Task<List<Article>> GetAllAdminAsync(int page = 1, int pageSize = 50) =>
        _repo.GetAllAsync((page - 1) * pageSize, pageSize);

    public async Task<Article> CreateAsync(ArticleDto dto)
    {
        if (await _repo.SlugExistsAsync(dto.Slug))
            throw new InvalidOperationException($"El slug '{dto.Slug}' ya está en uso.");

        var status = Enum.Parse<ArticleStatus>(dto.Status, ignoreCase: true);
        var article = new Article
        {
            Title = dto.Title,
            Slug = dto.Slug,
            Summary = dto.Summary,
            Content = dto.Content,
            Author = dto.Author,
            CategoryId = NullIfEmpty(dto.CategoryId),
            SubcategoryId = NullIfEmpty(dto.SubcategoryId),
            Tags = dto.Tags,
            CoverImageId = NullIfEmpty(dto.CoverImageId),
            Featured = dto.Featured,
            Status = status,
            PublishedAt = status == ArticleStatus.Publicado ? DateTime.UtcNow : null,
            ReadingTimeMinutes = EstimateReadingTime(dto.Content)
        };
        await _repo.InsertAsync(article);
        return article;
    }

    public async Task<Article> UpdateAsync(string id, ArticleDto dto)
    {
        var existing = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Artículo '{id}' no encontrado.");

        if (await _repo.SlugExistsAsync(dto.Slug, id))
            throw new InvalidOperationException($"El slug '{dto.Slug}' ya está en uso.");

        var status = Enum.Parse<ArticleStatus>(dto.Status, ignoreCase: true);

        existing.Title = dto.Title;
        existing.Slug = dto.Slug;
        existing.Summary = dto.Summary;
        existing.Content = dto.Content;
        existing.Author = dto.Author;
        existing.CategoryId = NullIfEmpty(dto.CategoryId);
        existing.SubcategoryId = NullIfEmpty(dto.SubcategoryId);
        existing.Tags = dto.Tags;
        existing.CoverImageId = NullIfEmpty(dto.CoverImageId);
        existing.Featured = dto.Featured;
        existing.Status = status;
        existing.ReadingTimeMinutes = EstimateReadingTime(dto.Content);

        if (status == ArticleStatus.Publicado && existing.PublishedAt is null)
            existing.PublishedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(existing);
        return existing;
    }

    public async Task DeleteAsync(string id)
    {
        _ = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Artículo '{id}' no encontrado.");
        await _repo.DeleteAsync(id);
    }

    public Task IncrementViewCountAsync(string id) => _repo.IncrementViewCountAsync(id);

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static int EstimateReadingTime(string htmlContent)
    {
        var words = System.Text.RegularExpressions.Regex
            .Replace(htmlContent, "<[^>]+>", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / 200.0));
    }
}
