using Aplication.DTOs.ContentBlocks;

namespace Aplication.DTOs;

/// <summary>DTO de respuesta de una pagina informativa.</summary>
public class PageResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public List<ContentBlockDto> Blocks { get; set; } = [];
    public string Subtitle { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool ShowInFooter { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
