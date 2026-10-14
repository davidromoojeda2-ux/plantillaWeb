using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Mappers;

namespace Aplication.Services;

public interface ICategoryService
{
    Task<List<CategoryResponseDto>> GetAllAsync();
    Task<CategoryResponseDto?> GetByIdAsync(string id);
    Task<CategoryResponseDto?> GetBySlugAsync(string slug);
    Task<bool> SlugExistsAsync(string slug, string? excludeId = null);
    Task<CategoryResponseDto> CreateAsync(CategoryDto dto);
    Task<CategoryResponseDto> UpdateAsync(string id, CategoryDto dto);
    Task DeleteAsync(string id);
}

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;

    public CategoryService(ICategoryRepository repo) => _repo = repo;

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

    public Task<bool> SlugExistsAsync(string slug, string? excludeId = null) =>
        _repo.SlugExistsAsync(slug, excludeId);

    public async Task<CategoryResponseDto> CreateAsync(CategoryDto dto)
    {
        if (await _repo.SlugExistsAsync(dto.Slug))
            throw new InvalidOperationException($"El slug '{dto.Slug}' ya está registrado.");

        var category = CategoryMapper.ToEntity(dto);
        await _repo.InsertAsync(category);
        return CategoryMapper.ToDto(category);
    }

    public async Task<CategoryResponseDto> UpdateAsync(string id, CategoryDto dto)
    {
        var existing = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Categoría '{id}' no encontrada.");

        if (await _repo.SlugExistsAsync(dto.Slug, id))
            throw new InvalidOperationException($"El slug '{dto.Slug}' ya está en uso.");

        CategoryMapper.ApplyUpdate(existing, dto);
        await _repo.UpdateAsync(existing);
        return CategoryMapper.ToDto(existing);
    }

    public Task DeleteAsync(string id) => _repo.DeleteAsync(id);
}
