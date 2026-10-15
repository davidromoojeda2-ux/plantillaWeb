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
