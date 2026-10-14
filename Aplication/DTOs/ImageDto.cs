using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Aplication.DTOs;

/// <summary>DTO de subida de imagen al sistema de almacenamiento.</summary>
public class ImageUploadDto
{
    [Required(ErrorMessage = "Debes seleccionar un archivo.")]
    public IFormFile File { get; set; } = null!;

    [StringLength(200)]
    public string Alt { get; set; } = string.Empty;
}

/// <summary>DTO de respuesta para una variante WebP de imagen.</summary>
public class ImageVariantDto
{
    public int Width { get; set; }
    public string Key { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? PublicUrl { get; set; }
}

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
