using Aplication.Interfaces;
using Domine.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Aplication.Services;

/// <summary>
/// Modelo de menú de navegación: categorías raíz con sus subcategorías.
/// </summary>
public class MenuCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public List<MenuCategory> Children { get; set; } = [];
}

public interface IMenuService
{
    Task<List<MenuCategory>> GetMenuAsync();
    void InvalidateMenu();
}

/// <summary>
/// Carga el menú desde MongoDB con IMemoryCache (5 min).
/// En caso de fallo de DB, retorna la última copia en caché como fallback.
/// </summary>
public class MenuService : IMenuService
{
    private const string CacheKey = "nav_menu";
    private readonly IMemoryCache _cache;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IArticleRepository _articleRepo;

    public MenuService(
        IMemoryCache cache,
        ICategoryRepository categoryRepo,
        IArticleRepository articleRepo)
    {
        _cache = cache;
        _categoryRepo = categoryRepo;
        _articleRepo = articleRepo;
    }

    public async Task<List<MenuCategory>> GetMenuAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<MenuCategory>? cached) && cached is not null)
            return cached;

        try
        {
            var all = await _categoryRepo.GetMenuCategoriesAsync();

            var menu = all
                .Where(c => c.ParentId is null && c.IsActive && c.ShowInMenu)
                .OrderBy(c => c.Order)
                .Select(r => new MenuCategory
                {
                    Id = r.Id,
                    Name = r.Name,
                    Slug = r.Slug,
                    Children = all
                        .Where(c => c.ParentId == r.Id && c.IsActive)
                        .OrderBy(c => c.Order)
                        .Select(c => new MenuCategory
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Slug = c.Slug
                        })
                        .ToList()
                })
                .ToList();

            _cache.Set(CacheKey, menu, TimeSpan.FromMinutes(5));
            return menu;
        }
        catch
        {
            return cached ?? [];
        }
    }

    public void InvalidateMenu() => _cache.Remove(CacheKey);
}
