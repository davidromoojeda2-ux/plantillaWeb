using Aplication.DTOs;
using Domine.Entities;

namespace Aplication.Mappers;

public static class CategoryMapper
{
    public static Category ToEntity(CategoryDto dto) => new()
    {
        Name = dto.Name.Trim(),
        Slug = dto.Slug.Trim().ToLowerInvariant(),
        ParentId = NullIfEmpty(dto.ParentId),
        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
        Order = dto.Order,
        ShowInMenu = dto.ShowInMenu,
        IsActive = dto.IsActive
    };

    public static Category ApplyUpdate(Category existing, CategoryDto dto)
    {
        existing.Name = dto.Name.Trim();
        existing.Slug = dto.Slug.Trim().ToLowerInvariant();
        existing.ParentId = NullIfEmpty(dto.ParentId);
        existing.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        existing.Order = dto.Order;
        existing.ShowInMenu = dto.ShowInMenu;
        existing.IsActive = dto.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        return existing;
    }

    public static CategoryResponseDto ToDto(Category cat) => new()
    {
        Id = cat.Id,
        ParentId = cat.ParentId,
        Name = cat.Name,
        Slug = cat.Slug,
        Description = cat.Description,
        Order = cat.Order,
        ShowInMenu = cat.ShowInMenu,
        IsActive = cat.IsActive,
        CreatedAt = cat.CreatedAt,
        UpdatedAt = cat.UpdatedAt
    };

    public static List<CategoryResponseDto> ToDtoList(IEnumerable<Category> cats) =>
        cats.Select(ToDto).ToList();

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
