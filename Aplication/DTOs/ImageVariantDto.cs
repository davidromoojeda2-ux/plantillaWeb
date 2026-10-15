namespace Aplication.DTOs;

/// <summary>DTO de respuesta para una variante WebP de imagen.</summary>
public class ImageVariantDto
{
    public int Width { get; set; }
    public string Key { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? PublicUrl { get; set; }
}
