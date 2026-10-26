using Aplication.DTOs;

namespace paginaWeb.Areas.Admin.Models;

/// <summary>
/// Modelo de vista para la biblioteca de medios e imágenes del CMS.
/// </summary>
public class ImageLibraryViewModel
{
    public List<ImageMetadataResponseDto> Images { get; set; } = [];
    public List<ArticleResponseDto> Articles { get; set; } = [];
    public StorageStatusDto StorageStatus { get; set; } = new();

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public long TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public string? Search { get; set; }
    public string? StorageFilter { get; set; }
    public string? ArticleFilter { get; set; }
}
