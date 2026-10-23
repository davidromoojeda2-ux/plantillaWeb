using System.ComponentModel.DataAnnotations;

namespace Aplication.DTOs;

/// <summary>DTO de inicio de sesion del administrador.</summary>
public class AdminLoginDto
{
    [StringLength(20, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 20 caracteres.")]
    [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "El nombre de usuario solo puede contener letras y números.")]
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    public string Username { get; set; } = string.Empty;

    [StringLength(20, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 20 caracteres.")]
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}
