using Aplication.DTOs;
using Domine.Entities;

namespace Aplication.Mappers;

public static class CommentMapper
{
    public static Comment ToEntity(CommentSubmissionDto dto, string visitorId) => new()
    {
        ArticleId = dto.ArticleId,
        VisitorId = visitorId,
        Name = dto.Name.Trim(),
        Content = dto.Content.Trim(),
        Status = CommentStatus.Pendiente
    };

    public static CommentResponseDto ToDto(Comment comment) => new()
    {
        Id = comment.Id,
        ArticleId = comment.ArticleId,
        VisitorId = comment.VisitorId,
        Name = comment.Name,
        Content = comment.Content,
        Status = comment.Status.ToString(),
        CreatedAt = comment.CreatedAt
    };

    public static List<CommentResponseDto> ToDtoList(IEnumerable<Comment> comments) =>
        comments.Select(ToDto).ToList();
}
