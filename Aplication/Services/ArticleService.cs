using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Mappers;

namespace Aplication.Services;

public interface IArticleService
{
    Task<(List<ArticleResponseDto> Items, long Total)> GetPublishedAsync(int page = 1, int pageSize = 9);
    Task<(List<ArticleResponseDto> Items, long Total)> GetByCategoryAsync(string categoryId, int page = 1, int pageSize = 9);
    Task<(List<ArticleResponseDto> Items, long Total)> SearchAsync(string query, int page = 1, int pageSize = 9);
    Task<List<ArticleResponseDto>> GetFeaturedAsync(int take = 3);
    Task<ArticleResponseDto?> GetBySlugAsync(string slug);
    Task<ArticleResponseDto?> GetByIdAsync(string id);
    Task<List<ArticleResponseDto>> GetAllAdminAsync(int page = 1, int pageSize = 50);
    Task<ArticleResponseDto> CreateAsync(ArticleDto dto);
    Task<ArticleResponseDto> UpdateAsync(string id, ArticleDto dto);
    Task DeleteAsync(string id);
    Task IncrementViewCountAsync(string id);
}

public class ArticleService : IArticleService
{
    private readonly IArticleRepository _repo;

    public ArticleService(IArticleRepository repo) => _repo = repo;

    public async Task<(List<ArticleResponseDto> Items, long Total)> GetPublishedAsync(int page = 1, int pageSize = 9)
    {
        var skip = (page - 1) * pageSize;
        var items = await _repo.GetPublishedAsync(skip, pageSize);
        var total = await _repo.CountPublishedAsync();
        return (ArticleMapper.ToDtoList(items), total);
    }

    public async Task<(List<ArticleResponseDto> Items, long Total)> GetByCategoryAsync(string categoryId, int page = 1, int pageSize = 9)
    {
        var skip = (page - 1) * pageSize;
        var items = await _repo.GetByCategoryAsync(categoryId, skip, pageSize);
        var total = await _repo.CountByCategoryAsync(categoryId);
        return (ArticleMapper.ToDtoList(items), total);
    }

    public async Task<(List<ArticleResponseDto> Items, long Total)> SearchAsync(string query, int page = 1, int pageSize = 9)
    {
        var skip = (page - 1) * pageSize;
        var items = await _repo.SearchAsync(query, skip, pageSize);
        var total = await _repo.CountSearchAsync(query);
        return (ArticleMapper.ToDtoList(items), total);
    }

    public async Task<List<ArticleResponseDto>> GetFeaturedAsync(int take = 3) =>
        ArticleMapper.ToDtoList(await _repo.GetFeaturedAsync(take));

    public async Task<ArticleResponseDto?> GetBySlugAsync(string slug)
    {
        var article = await _repo.GetBySlugAsync(slug);
        return article is not null ? ArticleMapper.ToDto(article) : null;
    }

    public async Task<ArticleResponseDto?> GetByIdAsync(string id)
    {
        var article = await _repo.GetByIdAsync(id);
        return article is not null ? ArticleMapper.ToDto(article) : null;
    }

    public async Task<List<ArticleResponseDto>> GetAllAdminAsync(int page = 1, int pageSize = 50) =>
        ArticleMapper.ToDtoList(await _repo.GetAllAsync((page - 1) * pageSize, pageSize));

    public async Task<ArticleResponseDto> CreateAsync(ArticleDto dto)
    {
        if (await _repo.SlugExistsAsync(dto.Slug))
            throw new InvalidOperationException($"El slug '{dto.Slug}' ya está en uso.");

        var article = ArticleMapper.ToEntity(dto);
        await _repo.InsertAsync(article);
        return ArticleMapper.ToDto(article);
    }

    public async Task<ArticleResponseDto> UpdateAsync(string id, ArticleDto dto)
    {
        var existing = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Artículo '{id}' no encontrado.");

        if (await _repo.SlugExistsAsync(dto.Slug, id))
            throw new InvalidOperationException($"El slug '{dto.Slug}' ya está en uso.");

        ArticleMapper.ApplyUpdate(existing, dto);
        await _repo.UpdateAsync(existing);
        return ArticleMapper.ToDto(existing);
    }

    public async Task DeleteAsync(string id)
    {
        _ = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Artículo '{id}' no encontrado.");
        await _repo.DeleteAsync(id);
    }

    public Task IncrementViewCountAsync(string id) => _repo.IncrementViewCountAsync(id);
}
