using Aplication.DTOs;
using Domine.Content;
using Domine.ContentBlocks;
using Domine.Entities;

namespace Aplication.Mappers;

public static class PageMapper
{
    public static InformativePage ToEntity(PageDto dto, IReadOnlyList<ContentBlock> blocks) => new()
    {
        Title = dto.Title.Trim(),
        Slug = dto.Slug.Trim().ToLowerInvariant(),
        Blocks = [.. blocks],
        SchemaVersion = ContentLimits.CurrentSchemaVersion,
        Subtitle = dto.Subtitle.Trim(),
        Order = dto.Order,
        ShowInFooter = dto.ShowInFooter
    };

    public static InformativePage ApplyUpdate(InformativePage existing, PageDto dto, IReadOnlyList<ContentBlock> blocks)
    {
        existing.Title = dto.Title.Trim();
        existing.Slug = dto.Slug.Trim().ToLowerInvariant();
        existing.Blocks = [.. blocks];
        existing.SchemaVersion = ContentLimits.CurrentSchemaVersion;
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
        Blocks = ContentBlockMapper.ToDtoList(page.Blocks),
        Subtitle = page.Subtitle,
        Order = page.Order,
        ShowInFooter = page.ShowInFooter,
        CreatedAt = page.CreatedAt,
        UpdatedAt = page.UpdatedAt
    };

    public static List<PageResponseDto> ToDtoList(IEnumerable<InformativePage> pages) =>
        pages.Select(ToDto).ToList();
}
