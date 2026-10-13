using System.Security.Cryptography;
using System.Text;
using Aplication.DTOs;
using Aplication.Interfaces;
using Domine.Entities;

namespace Aplication.Services;

// ─── Comment Service ──────────────────────────────────────────────────────────

public interface ICommentService
{
    Task<List<Comment>> GetApprovedByArticleAsync(string articleId);
    Task<List<Comment>> GetPendingAsync();
    Task<List<Comment>> GetAllAsync();
    Task SubmitAsync(CommentSubmissionDto dto, string visitorIp);
    Task ApproveAsync(string id);
    Task RejectAsync(string id);
    Task DeleteAsync(string id);
}

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepo;
    private readonly IVisitorRepository _visitorRepo;

    public CommentService(ICommentRepository commentRepo, IVisitorRepository visitorRepo)
    {
        _commentRepo = commentRepo;
        _visitorRepo = visitorRepo;
    }

    public Task<List<Comment>> GetApprovedByArticleAsync(string articleId) =>
        _commentRepo.GetByArticleAsync(articleId, CommentStatus.Aprobado);

    public Task<List<Comment>> GetPendingAsync() => _commentRepo.GetPendingAsync();
    public Task<List<Comment>> GetAllAsync() => _commentRepo.GetAllAsync();

    public async Task SubmitAsync(CommentSubmissionDto dto, string visitorIp)
    {
        if (!string.IsNullOrWhiteSpace(dto.Trap)) return;

        var ipHash = HashIp(visitorIp);
        var visitor = await _visitorRepo.GetByEmailAsync(dto.Email);
        if (visitor is null)
        {
            visitor = new Visitor { Name = dto.Name, Email = dto.Email, IpHash = ipHash };
            await _visitorRepo.InsertAsync(visitor);
        }

        await _commentRepo.InsertAsync(new Comment
        {
            ArticleId = dto.ArticleId,
            VisitorId = visitor.Id,
            Name = dto.Name,
            Content = dto.Content,
            Status = CommentStatus.Pendiente
        });
    }

    public Task ApproveAsync(string id) => _commentRepo.UpdateStatusAsync(id, CommentStatus.Aprobado);
    public Task RejectAsync(string id) => _commentRepo.UpdateStatusAsync(id, CommentStatus.Rechazado);
    public Task DeleteAsync(string id) => _commentRepo.DeleteAsync(id);

    private static string HashIp(string ip) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(ip)));
}

// ─── Page Service ─────────────────────────────────────────────────────────────

public interface IPageService
{
    Task<List<InformativePage>> GetAllAsync();
    Task<List<InformativePage>> GetFooterPagesAsync();
    Task<InformativePage?> GetBySlugAsync(string slug);
    Task<InformativePage?> GetByIdAsync(string id);
    Task<InformativePage> CreateAsync(PageDto dto);
    Task<InformativePage> UpdateAsync(string id, PageDto dto);
    Task DeleteAsync(string id);
}

public class PageService : IPageService
{
    private readonly IInformativePageRepository _repo;

    public PageService(IInformativePageRepository repo) => _repo = repo;

    public Task<List<InformativePage>> GetAllAsync() => _repo.GetAllAsync();
    public Task<List<InformativePage>> GetFooterPagesAsync() => _repo.GetFooterPagesAsync();
    public Task<InformativePage?> GetBySlugAsync(string slug) => _repo.GetBySlugAsync(slug);
    public Task<InformativePage?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);

    public async Task<InformativePage> CreateAsync(PageDto dto)
    {
        var page = new InformativePage
        {
            Title = dto.Title, Slug = dto.Slug, Content = dto.Content,
            Order = dto.Order, ShowInFooter = dto.ShowInFooter
        };
        await _repo.InsertAsync(page);
        return page;
    }

    public async Task<InformativePage> UpdateAsync(string id, PageDto dto)
    {
        var page = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Sección '{id}' no encontrada.");
        page.Title = dto.Title; page.Slug = dto.Slug; page.Content = dto.Content;
        page.Order = dto.Order; page.ShowInFooter = dto.ShowInFooter;
        await _repo.UpdateAsync(page);
        return page;
    }

    public Task DeleteAsync(string id) => _repo.DeleteAsync(id);
}

// ─── Auth Service (interfaz; implementación en Infrastructure.Auth) ───────────

public interface IAuthService
{
    Task<bool> ValidateCredentialsAsync(string username, string password);
    Task<AdminUser?> GetUserAsync(string username);
}
