using Aplication.Configuration;
using Aplication.DTOs;

namespace paginaWeb.Areas.Admin.Models;

/// <summary>Modelo de la biblioteca de documentos del CMS.</summary>
public class DocumentLibraryViewModel
{
    public List<DocumentResponseDto> Documents { get; set; } = [];
    public List<ArticleResponseDto> Articles { get; set; } = [];
    public List<string> AllowedExtensions { get; set; } = [];

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public long TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public string? Search { get; set; }
    public string? StorageFilter { get; set; }
    public string? ExtensionFilter { get; set; }
    public string? ArticleFilter { get; set; }

    public static List<string> DefaultExtensions =>
        new DocumentsOptions().NormalizedExtensions().OrderBy(extension => extension).ToList();
}
