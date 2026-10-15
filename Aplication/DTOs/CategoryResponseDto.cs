namespace Aplication.DTOs;

/// <summary>DTO de respuesta para una categoria, incluye id y timestamps.</summary>
public class CategoryResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool ShowInMenu { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
