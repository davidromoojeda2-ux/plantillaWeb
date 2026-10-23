using Aplication.DTOs;
using Domine.Entities;

namespace Aplication.Mappers;

public static class PageMapper
{
    public static InformativePage ToEntity(PageDto dto) => new()
    {
        Title = dto.Title.Trim(),
        Slug = dto.Slug.Trim().ToLowerInvariant(),
        Content = dto.Content,
        Subtitle = dto.Subtitle.Trim(),
        Order = dto.Order,
        ShowInFooter = dto.ShowInFooter
    };

    public static InformativePage ApplyUpdate(InformativePage existing, PageDto dto)
    {
        existing.Title = dto.Title.Trim();
        existing.Slug = dto.Slug.Trim().ToLowerInvariant();
        existing.Content = dto.Content;
        existing.Subtitle = dto.Subtitle.Trim();
        existing.Order = dto.Order;
        existing.ShowInFooter = dto.ShowInFooter;
        existing.UpdatedAt = DateTime.UtcNow;
        return existing;
    }

    public static PageResponseDto ToDto(InformativePage page) => new()
    {
        Id = page.Id,
        Title = page.Title,
        Slug = page.Slug,
        Content = page.Content,
        Subtitle = page.Subtitle,
        Order = page.Order,
        ShowInFooter = page.ShowInFooter,
        CreatedAt = page.CreatedAt,
        UpdatedAt = page.UpdatedAt
    };

    public static List<PageResponseDto> ToDtoList(IEnumerable<InformativePage> pages) =>
        pages.Select(ToDto).ToList();

    public static PageDto ToEditDto(InformativePage page) => new()
    {
        Title = page.Title,
        Slug = page.Slug,
        Content = page.Content,
        Subtitle = page.Subtitle,
        Order = page.Order,
        ShowInFooter = page.ShowInFooter
    };

    public static PageDto ToEditDto(PageResponseDto page) => new()
    {
        Title = page.Title,
        Slug = page.Slug,
        Content = page.Content,
        Subtitle = page.Subtitle,
        Order = page.Order,
        ShowInFooter = page.ShowInFooter
    };
}
