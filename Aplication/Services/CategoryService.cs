using Aplication.DTOs;
using Aplication.Exceptions;
using Aplication.Interfaces;
using Aplication.Mappers;
using Aplication.Realtime;

namespace Aplication.Services;

public interface ICategoryService
{
    Task<List<CategoryResponseDto>> GetAllAsync();
    Task<CategoryResponseDto?> GetByIdAsync(string id);
    Task<CategoryResponseDto?> GetBySlugAsync(string slug);
    Task<List<CategoryResponseDto>> GetFooterSectionsAsync(int take = 4);
    Task<bool> SlugExistsAsync(string slug, string? excludeId = null);
    Task<CategoryResponseDto> CreateAsync(CategoryDto dto);
    Task<CategoryResponseDto> UpdateAsync(string id, CategoryDto dto);
    Task DeleteAsync(string id);
}

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;
    private readonly IArticleRepository _articleRepo;
    private readonly ICmsChangePublisher? _changes;

    public CategoryService(
        ICategoryRepository repo,
        IArticleRepository articleRepo,
        ICmsChangePublisher? changes = null)
    {
        _repo = repo;
        _articleRepo = articleRepo;
        _changes = changes;
    }

    public async Task<List<CategoryResponseDto>> GetAllAsync() =>
        CategoryMapper.ToDtoList(await _repo.GetAllAsync());

    public async Task<CategoryResponseDto?> GetByIdAsync(string id)
    {
        var category = await _repo.GetByIdAsync(id);
        return category is not null ? CategoryMapper.ToDto(category) : null;
    }

    public async Task<CategoryResponseDto?> GetBySlugAsync(string slug)
    {
        var category = await _repo.GetBySlugAsync(slug);
        return category is not null ? CategoryMapper.ToDto(category) : null;
    }

    /// <summary>
    /// Devuelve las secciones raíz activas con mayor audiencia (vistas acumuladas)
    /// y, en empate, con mayor volumen de artículos publicados. Incluye las vistas
    /// de sus subcategorías sin contar dos veces un mismo artículo.
    /// </summary>
    public async Task<List<CategoryResponseDto>> GetFooterSectionsAsync(int take = 4)
    {
        if (take <= 0) return [];

        var categories = (await _repo.GetAllAsync())
            .Where(c => c.IsActive)
            .ToList();

        if (categories.Count == 0) return [];

        var byId = categories.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        var signals = await _articleRepo.GetPublishedCategorySignalsAsync();
        var stats = new Dictionary<string, SectionStat>(StringComparer.OrdinalIgnoreCase);

        foreach (var signal in signals)
        {
            var rootIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddRootId(signal.CategoryId);
            AddRootId(signal.SubcategoryId);

            foreach (var rootId in rootIds)
            {
                if (!stats.TryGetValue(rootId, out var stat))
                    stat = new SectionStat();

                stat.ArticleCount += 1;
                stat.TotalViews += signal.ViewCount;
                stats[rootId] = stat;
            }

            void AddRootId(string? categoryId)
            {
                if (string.IsNullOrWhiteSpace(categoryId) || !byId.TryGetValue(categoryId, out var category))
                    return;

                var rootId = category.ParentId ?? category.Id;
                if (!string.IsNullOrEmpty(rootId) &&
                    byId.TryGetValue(rootId, out var root) &&
                    root.ParentId is null)
                {
                    rootIds.Add(root.Id);
                }
            }
        }

        return categories
            .Where(c => c.ParentId is null && stats.ContainsKey(c.Id))
            .OrderByDescending(c => stats[c.Id].TotalViews)
            .ThenByDescending(c => stats[c.Id].ArticleCount)
            .ThenBy(c => c.Order)
            .Take(take)
            .Select(CategoryMapper.ToDto)
            .ToList();
    }

    public Task<bool> SlugExistsAsync(string slug, string? excludeId = null) =>
        _repo.SlugExistsAsync(slug, excludeId);

    public async Task<CategoryResponseDto> CreateAsync(CategoryDto dto)
    {
        var category = CategoryMapper.ToEntity(dto);
        category.ValidarInvariantes();

        if (await _repo.SlugExistsAsync(category.Slug))
            throw new DuplicateSlugException(category.Slug);

        await _repo.InsertAsync(category);
        _changes?.Publish(CmsModule.Categories, category.Id, CmsChangeOperation.Created);
        return CategoryMapper.ToDto(category);
    }

    public async Task<CategoryResponseDto> UpdateAsync(string id, CategoryDto dto)
    {
        var existing = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Categoría", id);

        existing.AsignarPadre(dto.ParentId);
        CategoryMapper.ApplyUpdate(existing, dto);
        existing.ValidarInvariantes();

        if (await _repo.SlugExistsAsync(existing.Slug, id))
            throw new DuplicateSlugException(existing.Slug);

        await _repo.UpdateAsync(existing);
        _changes?.Publish(CmsModule.Categories, existing.Id, CmsChangeOperation.Updated);
        return CategoryMapper.ToDto(existing);
    }

    public async Task DeleteAsync(string id)
    {
        _ = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Categoría", id);
        await _repo.DeleteAsync(id);
        _changes?.Publish(CmsModule.Categories, id, CmsChangeOperation.Deleted);
    }

    private sealed class SectionStat
    {
        public long ArticleCount { get; set; }
        public long TotalViews { get; set; }
    }
}
