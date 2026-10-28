using Aplication.DTOs;
using Aplication.Exceptions;
using Aplication.Interfaces;
using Aplication.Mappers;

namespace Aplication.Services;

public interface IArticleService
{
    Task<(List<ArticleResponseDto> Items, long Total)> GetPublishedAsync(int page = 1, int pageSize = 9);
    Task<List<ArticleResponseDto>> GetAllPublishedAsync();
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
    Task<List<ArticleResponseDto>> GetRecommendedAsync(string currentArticleId, string? categoryId, int take = 3);
    Task<List<ArticleResponseDto>> GetMostReadAsync(int take = 5);
    Task<HashSet<string>> GetCategoriesWithPublishedArticlesAsync();
    Task<(ArticleResponseDto? Previous, ArticleResponseDto? Next)> GetAdjacentArticlesAsync(string currentArticleId, DateTime? publishedAt);
}

public class ArticleService : IArticleService
{
    private readonly IArticleRepository _repo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ContentBodySanitizer _contentSanitizer;

    public ArticleService(
        IArticleRepository repo,
        ICategoryRepository categoryRepo,
        ContentBodySanitizer contentSanitizer)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _contentSanitizer = contentSanitizer;
    }

    public Task<HashSet<string>> GetCategoriesWithPublishedArticlesAsync() =>
        _repo.GetCategoriesWithPublishedArticlesAsync();

    public async Task<(List<ArticleResponseDto> Items, long Total)> GetPublishedAsync(int page = 1, int pageSize = 9)
    {
        var skip = (page - 1) * pageSize;
        var items = await _repo.GetPublishedAsync(skip, pageSize);
        var total = await _repo.CountPublishedAsync();
        return (ArticleMapper.ToDtoList(items), total);
    }

    public async Task<List<ArticleResponseDto>> GetAllPublishedAsync() =>
        ArticleMapper.ToDtoList(await _repo.GetAllPublishedAsync());

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
        await EnforceCategoryIntegrityAsync(dto);

        var blocks = _contentSanitizer.Sanitize(dto.Blocks);
        var article = ArticleMapper.ToEntity(dto, blocks);
        ApplyDerivedFields(article);
        article.ValidarInvariantes();

        if (await _repo.SlugExistsAsync(article.Slug))
            throw new DuplicateSlugException(article.Slug);

        await _repo.InsertAsync(article);
        return ArticleMapper.ToDto(article);
    }

    public async Task<ArticleResponseDto> UpdateAsync(string id, ArticleDto dto)
    {
        await EnforceCategoryIntegrityAsync(dto);

        var existing = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Artículo", id);

        var blocks = _contentSanitizer.Sanitize(dto.Blocks);
        ArticleMapper.ApplyUpdate(existing, dto, blocks);
        ApplyDerivedFields(existing);
        existing.ValidarInvariantes();

        if (await _repo.SlugExistsAsync(existing.Slug, id))
            throw new DuplicateSlugException(existing.Slug);

        await _repo.UpdateAsync(existing);
        return ArticleMapper.ToDto(existing);
    }

    private static void ApplyDerivedFields(Domine.Entities.Article article)
    {
        article.SearchText = article.BuildSearchText();
        article.ReadingTimeMinutes = Domine.Entities.Article.EstimateReadingTime(article.SearchText);
    }

    private async Task EnforceCategoryIntegrityAsync(ArticleDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.SubcategoryId))
        {
            var subcat = await _categoryRepo.GetByIdAsync(dto.SubcategoryId.Trim());
            if (subcat != null)
            {
                if (!string.IsNullOrWhiteSpace(subcat.ParentId))
                {
                    dto.CategoryId = subcat.ParentId;
                    dto.SubcategoryId = subcat.Id;
                }
                else
                {
                    dto.CategoryId = subcat.Id;
                    dto.SubcategoryId = null;
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(dto.CategoryId))
        {
            var cat = await _categoryRepo.GetByIdAsync(dto.CategoryId.Trim());
            if (cat != null)
            {
                if (!string.IsNullOrWhiteSpace(cat.ParentId))
                {
                    dto.CategoryId = cat.ParentId;
                    dto.SubcategoryId = cat.Id;
                }
                else
                {
                    dto.CategoryId = cat.Id;
                    dto.SubcategoryId = null;
                }
            }
        }
    }

    public async Task DeleteAsync(string id)
    {
        _ = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Artículo", id);
        await _repo.DeleteAsync(id);
    }

    public Task IncrementViewCountAsync(string id) => _repo.IncrementViewCountAsync(id);

    public async Task<List<ArticleResponseDto>> GetRecommendedAsync(string currentArticleId, string? categoryId, int take = 3)
    {
        var items = await _repo.GetRecommendedAsync(currentArticleId, categoryId, take);
        return ArticleMapper.ToDtoList(items);
    }

    public async Task<List<ArticleResponseDto>> GetMostReadAsync(int take = 5)
    {
        var items = await _repo.GetMostReadAsync(take);
        return ArticleMapper.ToDtoList(items);
    }

    public async Task<(ArticleResponseDto? Previous, ArticleResponseDto? Next)> GetAdjacentArticlesAsync(string currentArticleId, DateTime? publishedAt)
    {
        var (prev, next) = await _repo.GetAdjacentArticlesAsync(currentArticleId, publishedAt);
        return (prev != null ? ArticleMapper.ToDto(prev) : null,
                next != null ? ArticleMapper.ToDto(next) : null);
    }
}
