using System.ComponentModel.DataAnnotations;

namespace Aplication.DTOs;

/// <summary>DTO de inicio de sesion del administrador.</summary>
public class AdminLoginDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}
