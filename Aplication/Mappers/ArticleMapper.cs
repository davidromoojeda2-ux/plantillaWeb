using Aplication.DTOs;
using Domine.Content;
using Domine.ContentBlocks;
using Domine.Entities;

namespace Aplication.Mappers;

/// <summary>
/// Convierte entre Article (entidad Domine) y los DTOs de articulo.
/// </summary>
public static class ArticleMapper
{
    public static Article ToEntity(ArticleDto dto, IReadOnlyList<ContentBlock> blocks)
    {
        var status = ParseStatus(dto.Status);
        return new Article
        {
            Title = dto.Title.Trim(),
            Slug = dto.Slug.Trim().ToLowerInvariant(),
            Summary = dto.Summary.Trim(),
            Blocks = [.. blocks],
            SchemaVersion = ContentLimits.CurrentSchemaVersion,
            Author = dto.Author.Trim(),
            AuthorBio = NullIfEmpty(dto.AuthorBio),
            AuthorAvatarUrl = NullIfEmpty(dto.AuthorAvatarUrl),
            AuthorRole = !string.IsNullOrWhiteSpace(dto.AuthorRole) ? dto.AuthorRole.Trim() : "Redacción Especial",
            AuthorSocialUrl = NullIfEmpty(dto.AuthorSocialUrl),
            CategoryId = NullIfEmpty(dto.CategoryId),
            SubcategoryId = NullIfEmpty(dto.SubcategoryId),
            Tags = dto.Tags.Select(t => t.Trim()).Where(t => t.Length > 0).ToList(),
            CoverImageId = NullIfEmpty(dto.CoverImageId),
            CoverImageUrl = NullIfEmpty(dto.CoverImageUrl),
            ImageIds = MergeImageIds(dto.ImageIds, blocks),
            Featured = dto.Featured,
            Status = status,
            PublishedAt = status == ArticleStatus.Publicado ? DateTime.UtcNow : null,
            ScheduledAt = status == ArticleStatus.Programado ? dto.ScheduledAt : null
        };
    }

    public static Article ApplyUpdate(Article existing, ArticleDto dto, IReadOnlyList<ContentBlock> blocks)
    {
        var status = ParseStatus(dto.Status);

        existing.Title = dto.Title.Trim();
        existing.Slug = dto.Slug.Trim().ToLowerInvariant();
        existing.Summary = dto.Summary.Trim();
        existing.Blocks = [.. blocks];
        existing.SchemaVersion = ContentLimits.CurrentSchemaVersion;
        existing.Author = dto.Author.Trim();
        existing.AuthorBio = NullIfEmpty(dto.AuthorBio);
        existing.AuthorAvatarUrl = NullIfEmpty(dto.AuthorAvatarUrl);
        existing.AuthorRole = !string.IsNullOrWhiteSpace(dto.AuthorRole) ? dto.AuthorRole.Trim() : "Redacción Especial";
        existing.AuthorSocialUrl = NullIfEmpty(dto.AuthorSocialUrl);
        existing.CategoryId = NullIfEmpty(dto.CategoryId);
        existing.SubcategoryId = NullIfEmpty(dto.SubcategoryId);
        existing.Tags = dto.Tags.Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
        existing.CoverImageId = NullIfEmpty(dto.CoverImageId);
        existing.CoverImageUrl = NullIfEmpty(dto.CoverImageUrl);
        existing.ImageIds = MergeImageIds(dto.ImageIds, blocks);
        existing.Featured = dto.Featured;
        existing.Status = status;
        existing.UpdatedAt = DateTime.UtcNow;

        if (status == ArticleStatus.Publicado && existing.PublishedAt is null)
            existing.PublishedAt = DateTime.UtcNow;

        return existing;
    }

    public static ArticleResponseDto ToDto(Article article) => new()
    {
        Id = article.Id,
        Title = article.Title,
        Slug = article.Slug,
        Summary = article.Summary,
        Blocks = ContentBlockMapper.ToDtoList(article.Blocks),
        Author = article.Author,
        AuthorBio = article.AuthorBio,
        AuthorAvatarUrl = article.AuthorAvatarUrl,
        AuthorRole = article.AuthorRole ?? "Redacción Especial",
        AuthorSocialUrl = article.AuthorSocialUrl,
        CategoryId = article.CategoryId,
        SubcategoryId = article.SubcategoryId,
        Tags = [.. article.Tags],
        CoverImageId = article.CoverImageId,
        CoverImageUrl = article.CoverImageUrl,
        ImageIds = [.. article.ImageIds],
        Featured = article.Featured,
        Status = article.Status.ToString(),
        PublishedAt = article.PublishedAt,
        ScheduledAt = article.ScheduledAt,
        CreatedAt = article.CreatedAt,
        UpdatedAt = article.UpdatedAt,
        ViewCount = article.ViewCount,
        ReadingTimeMinutes = article.ReadingTimeMinutes
    };

    public static List<ArticleResponseDto> ToDtoList(IEnumerable<Article> articles) =>
        articles.Select(ToDto).ToList();

    /// <summary>
    /// Mantiene el catálogo de imágenes del artículo sincronizado con las
    /// referencias de biblioteca presentes en los bloques del cuerpo.
    /// </summary>
    private static List<string> MergeImageIds(
        IEnumerable<string>? imageIds,
        IEnumerable<ContentBlock>? blocks) =>
        (imageIds ?? [])
            .Concat(blocks?.OfType<ContentImageBlock>().Select(block => block.ImageId) ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct()
            .ToList();

    public static ArticleDto ToEditDto(Article article) => ToEditDto(ToDto(article));

    public static ArticleDto ToEditDto(ArticleResponseDto article) => new()
    {
        Title = article.Title,
        Slug = article.Slug,
        Summary = article.Summary,
        Blocks = [.. article.Blocks],
        Author = article.Author,
        AuthorBio = article.AuthorBio,
        AuthorAvatarUrl = article.AuthorAvatarUrl,
        AuthorRole = article.AuthorRole,
        AuthorSocialUrl = article.AuthorSocialUrl,
        CategoryId = article.CategoryId,
        SubcategoryId = article.SubcategoryId,
        Tags = [.. article.Tags],
        CoverImageId = article.CoverImageId,
        CoverImageUrl = article.CoverImageUrl,
        ImageIds = [.. article.ImageIds],
        Featured = article.Featured,
        Status = article.Status,
        ScheduledAt = article.ScheduledAt
    };

    private static ArticleStatus ParseStatus(string status) =>
        Enum.TryParse<ArticleStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : ArticleStatus.Borrador;

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
