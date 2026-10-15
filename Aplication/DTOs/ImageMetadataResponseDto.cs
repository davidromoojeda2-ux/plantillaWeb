namespace Aplication.DTOs;

/// <summary>DTO de respuesta completa de metadatos de imagen optimizada.</summary>
public class ImageMetadataResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public int OriginalWidth { get; set; }
    public int OriginalHeight { get; set; }
    public List<ImageVariantDto> Variants { get; set; } = [];
    public string Storage { get; set; } = string.Empty;
    public string? ArticleId { get; set; }
    public string? ArticleTitle { get; set; }
    public DateTime UploadedAt { get; set; }
}
