using System.ComponentModel.DataAnnotations;

namespace Aplication.DTOs;

/// <summary>DTO de entrada para crear o editar un articulo editorial.</summary>
public class ArticleDto
{
    [Required(ErrorMessage = "El titulo es obligatorio.")]
    [StringLength(250, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "El resumen es obligatorio.")]
    [StringLength(500, MinimumLength = 20)]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "El contenido es obligatorio.")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "El autor es obligatorio.")]
    public string Author { get; set; } = string.Empty;

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

    /// <summary>"Borrador" o "Publicado"</summary>
    public string Status { get; set; } = "Borrador";
}

/// <summary>DTO de respuesta / lectura de un articulo, incluye campos calculados.</summary>
public class ArticleResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long ViewCount { get; set; }
    public int ReadingTimeMinutes { get; set; }
}
