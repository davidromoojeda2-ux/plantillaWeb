using System.ComponentModel.DataAnnotations;

namespace Aplication.DTOs;

// ─── Category ────────────────────────────────────────────────────────────────

public class CategoryDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "El slug solo puede contener letras minúsculas, números y guiones.")]
    public string Slug { get; set; } = string.Empty;

    public string? ParentId { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; } = 0;
    public bool ShowInMenu { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

// ─── Article ─────────────────────────────────────────────────────────────────

public class ArticleDto
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(250, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "El resumen es obligatorio.")]
    [StringLength(500, MinimumLength = 20)]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "El contenido es obligatorio.")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "El autor es obligatorio.")]
    public string Author { get; set; } = string.Empty;

    public string? CategoryId { get; set; }
    public string? SubcategoryId { get; set; }
    public List<string> Tags { get; set; } = [];
    public string? CoverImageId { get; set; }
    public bool Featured { get; set; } = false;

    /// <summary>"Borrador" o "Publicado"</summary>
    public string Status { get; set; } = "Borrador";
}

// ─── Comment ─────────────────────────────────────────────────────────────────

public class CommentSubmissionDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El comentario es obligatorio.")]
    [StringLength(2000, MinimumLength = 10)]
    public string Content { get; set; } = string.Empty;

    /// <summary>Campo honeypot antispam; debe enviarse vacío por clientes legítimos.</summary>
    public string? Trap { get; set; }

    [Required]
    public string ArticleId { get; set; } = string.Empty;
}

// ─── Admin Login ─────────────────────────────────────────────────────────────

public class AdminLoginDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

// ─── Informative Page ────────────────────────────────────────────────────────

public class PageDto
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "El contenido es obligatorio.")]
    public string Content { get; set; } = string.Empty;

    public int Order { get; set; } = 0;
    public bool ShowInFooter { get; set; } = true;
}

// ─── Image Upload ─────────────────────────────────────────────────────────────

public class ImageUploadDto
{
    [Required(ErrorMessage = "Debes seleccionar un archivo.")]
    public Microsoft.AspNetCore.Http.IFormFile File { get; set; } = null!;

    [StringLength(200)]
    public string Alt { get; set; } = string.Empty;
}
