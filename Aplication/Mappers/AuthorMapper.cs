using Aplication.DTOs;

namespace Aplication.Mappers;

/// <summary>
/// Provee métodos de mapeo especializados para el autor editorial entre
/// los DTOs de artículo y el modelo AuthorDto.
/// </summary>
public static class AuthorMapper
{
    public static AuthorDto ToAuthorDto(ArticleResponseDto article) => new()
    {
        Name = article.Author,
        Bio = article.AuthorBio,
        AvatarUrl = article.AuthorAvatarUrl,
        Role = article.AuthorRole ?? "Redacción Especial",
        SocialUrl = article.AuthorSocialUrl
    };
}
