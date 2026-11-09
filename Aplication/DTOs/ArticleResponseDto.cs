using Aplication.DTOs.ContentBlocks;

namespace Aplication.DTOs;

/// <summary>DTO de respuesta / lectura de un articulo, incluye campos calculados.</summary>
public class ArticleResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<ContentBlockDto> Blocks { get; set; } = [];
    public string Author { get; set; } = string.Empty;
    public string? AuthorBio { get; set; }
    public string? AuthorAvatarUrl { get; set; }
    public string? AuthorRole { get; set; }
    public string? AuthorSocialUrl { get; set; }
    public string? CategoryId { get; set; }
    public string? SubcategoryId { get; set; }
    public List<string> Tags { get; set; } = [];
    public string? CoverImageId { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<string> ImageIds { get; set; } = [];
    public bool Featured { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long ViewCount { get; set; }
    public int ReadingTimeMinutes { get; set; }
}
