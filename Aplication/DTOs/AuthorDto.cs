namespace Aplication.DTOs;

/// <summary>
/// DTO que representa el perfil y metadatos de un autor editorial.
/// </summary>
public class AuthorDto
{
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Role { get; set; } = "Redacción Especial";
    public string? SocialUrl { get; set; }
}
