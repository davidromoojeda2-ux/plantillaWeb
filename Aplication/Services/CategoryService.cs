using Aplication.DTOs;
using Aplication.Exceptions;
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
        var category = CategoryMapper.ToEntity(dto);
        category.ValidarInvariantes();

        if (await _repo.SlugExistsAsync(category.Slug))
            throw new DuplicateSlugException(category.Slug);

        await _repo.InsertAsync(category);
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
        return CategoryMapper.ToDto(existing);
    }

    public async Task DeleteAsync(string id)
    {
        _ = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Categoría", id);
        await _repo.DeleteAsync(id);
    }
}
