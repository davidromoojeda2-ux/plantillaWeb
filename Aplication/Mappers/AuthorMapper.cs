using Aplication.DTOs;
using Domine.Entities;

namespace Aplication.Mappers;

/// <summary>
/// Provee métodos de mapeo especializados para el autor editorial entre la entidad Article,
/// sus DTOs y el modelo AuthorDto.
/// </summary>
public static class AuthorMapper
{
    public static AuthorDto ToAuthorDto(Article article) => new()
    {
        Name = article.Author,
        Bio = article.AuthorBio,
        AvatarUrl = article.AuthorAvatarUrl,
        Role = article.AuthorRole ?? "Redacción Especial",
        SocialUrl = article.AuthorSocialUrl
    };

    public static AuthorDto ToAuthorDto(ArticleResponseDto article) => new()
    {
        Name = article.Author,
        Bio = article.AuthorBio,
        AvatarUrl = article.AuthorAvatarUrl,
        Role = article.AuthorRole ?? "Redacción Especial",
        SocialUrl = article.AuthorSocialUrl
    };

    public static void ApplyTo(AuthorDto author, Article article)
    {
        article.Author = author.Name.Trim();
        article.AuthorBio = string.IsNullOrWhiteSpace(author.Bio) ? null : author.Bio.Trim();
        article.AuthorAvatarUrl = string.IsNullOrWhiteSpace(author.AvatarUrl) ? null : author.AvatarUrl.Trim();
        article.AuthorRole = string.IsNullOrWhiteSpace(author.Role) ? "Redacción Especial" : author.Role.Trim();
        article.AuthorSocialUrl = string.IsNullOrWhiteSpace(author.SocialUrl) ? null : author.SocialUrl.Trim();
    }
}
