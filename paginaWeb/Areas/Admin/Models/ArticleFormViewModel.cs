using System.ComponentModel.DataAnnotations;
using Aplication.DTOs;
using Aplication.DTOs.ContentBlocks;
using Aplication.Serialization;

namespace paginaWeb.Areas.Admin.Models;

/// <summary>
/// Modelo de presentación (ViewModel) para la creación y edición de artículos en el panel administrativo.
/// Agrupa los datos del artículo, listas fuertemente tipadas de taxonomía e imágenes asociadas.
/// </summary>
public class ArticleFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(250, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 250 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    [StringLength(200, ErrorMessage = "El slug no puede superar los 200 caracteres.")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "El resumen es obligatorio.")]
    [StringLength(500, MinimumLength = 20, ErrorMessage = "El resumen debe tener entre 20 y 500 caracteres.")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>JSON serializado del editor de bloques; se valida en el controlador.</summary>
    [Required(ErrorMessage = "El cuerpo del artículo es obligatorio.")]
    public string BlocksJson { get; set; } = "[]";

    [Required(ErrorMessage = "El autor es obligatorio.")]
    [StringLength(100, ErrorMessage = "El autor no puede superar los 100 caracteres.")]
    public string Author { get; set; } = "Redacción";

    public string? AuthorBio { get; set; }

    public string? AuthorAvatarUrl { get; set; }

    public string? AuthorRole { get; set; } = "Redacción Especial";

    public string? AuthorSocialUrl { get; set; }

    public string? CategoryId { get; set; }

    public string? SubcategoryId { get; set; }

    public List<string> Tags { get; set; } = [];

    public string? CoverImageId { get; set; }

    public string? CoverImageUrl { get; set; }

    public List<string> ImageIds { get; set; } = [];

    public bool Featured { get; set; } = false;

    public string Status { get; set; } = "Publicado";

    // ─── Colecciones Fuertemente Tipadas para la Vista ───────────────────────

    public List<CategoryResponseDto> ParentCategories { get; set; } = [];

    public List<CategoryResponseDto> SubCategories { get; set; } = [];

    public List<ImageMetadataResponseDto> LinkedImages { get; set; } = [];

    public List<ImageMetadataResponseDto> AvailableImages { get; set; } = [];

    public ArticleDto ToDto(IReadOnlyList<ContentBlockDto> blocks) => new()
    {
        Title = Title,
        Slug = Slug,
        Summary = Summary,
        Blocks = [.. blocks],
        Author = Author,
        AuthorBio = AuthorBio,
        AuthorAvatarUrl = AuthorAvatarUrl,
        AuthorRole = AuthorRole,
        AuthorSocialUrl = AuthorSocialUrl,
        CategoryId = CategoryId,
        SubcategoryId = SubcategoryId,
        Tags = Tags,
        CoverImageId = CoverImageId,
        CoverImageUrl = CoverImageUrl,
        ImageIds = ImageIds,
        Featured = Featured,
        Status = Status
    };

    public static ArticleFormViewModel FromDto(ArticleResponseDto article, string? coverImageUrl = null) => new()
    {
        Id = article.Id,
        Title = article.Title,
        Slug = article.Slug,
        Summary = article.Summary,
        BlocksJson = ContentBlockJson.Serialize(article.Blocks),
        Author = article.Author,
        AuthorBio = article.AuthorBio,
        AuthorAvatarUrl = article.AuthorAvatarUrl,
        AuthorRole = article.AuthorRole,
        AuthorSocialUrl = article.AuthorSocialUrl,
        CategoryId = article.CategoryId,
        SubcategoryId = article.SubcategoryId,
        Tags = [.. article.Tags],
        CoverImageId = article.CoverImageId,
        CoverImageUrl = coverImageUrl ?? article.CoverImageUrl,
        ImageIds = [.. article.ImageIds],
        Featured = article.Featured,
        Status = article.Status
    };

    public static ArticleFormViewModel FromDto(
        ArticleResponseDto article,
        List<CategoryResponseDto> parentCategories,
        List<CategoryResponseDto> subCategories,
        string? coverImageUrl,
        List<ImageMetadataResponseDto> linkedImages,
        List<ImageMetadataResponseDto> availableImages)
    {
        var vm = FromDto(article, coverImageUrl);
        vm.ParentCategories = parentCategories;
        vm.SubCategories = subCategories;
        vm.LinkedImages = linkedImages;
        vm.AvailableImages = availableImages;
        return vm;
    }
}
