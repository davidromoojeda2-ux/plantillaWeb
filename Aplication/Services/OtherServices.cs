using System.Security.Cryptography;
using System.Text;
using Aplication.DTOs;
using Aplication.Exceptions;
using Aplication.Interfaces;
using Aplication.Mappers;
using Domine.Entities;

namespace Aplication.Services;

// ─── Comment Service ──────────────────────────────────────────────────────────

public interface ICommentService
{
    Task<List<CommentResponseDto>> GetApprovedByArticleAsync(string articleId);
    Task<List<CommentResponseDto>> GetPendingAsync();
    Task<List<CommentResponseDto>> GetAllAsync();
    Task<List<CommentResponseDto>> GetByStatusAsync(string? status);
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

    public async Task<List<CommentResponseDto>> GetApprovedByArticleAsync(string articleId) =>
        CommentMapper.ToDtoList(await _commentRepo.GetByArticleAsync(articleId, CommentStatus.Aprobado));

    public async Task<List<CommentResponseDto>> GetPendingAsync() =>
        CommentMapper.ToDtoList(await _commentRepo.GetPendingAsync());

    public async Task<List<CommentResponseDto>> GetAllAsync() =>
        CommentMapper.ToDtoList(await _commentRepo.GetAllAsync());

    public async Task<List<CommentResponseDto>> GetByStatusAsync(string? status)
    {
        List<Comment> comments;
        if (string.Equals(status, "aprobado", StringComparison.OrdinalIgnoreCase))
            comments = await _commentRepo.GetByStatusAsync(CommentStatus.Aprobado);
        else if (string.Equals(status, "rechazado", StringComparison.OrdinalIgnoreCase))
            comments = await _commentRepo.GetByStatusAsync(CommentStatus.Rechazado);
        else if (string.Equals(status, "todos", StringComparison.OrdinalIgnoreCase))
            comments = await _commentRepo.GetAllAsync();
        else
            comments = await _commentRepo.GetPendingAsync();

        return CommentMapper.ToDtoList(comments);
    }

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

        var comment = CommentMapper.ToEntity(dto, visitor.Id);
        await _commentRepo.InsertAsync(comment);
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
    Task<List<PageResponseDto>> GetAllAsync();
    Task<List<PageResponseDto>> GetFooterPagesAsync();
    Task<PageResponseDto?> GetBySlugAsync(string slug);
    Task<PageResponseDto?> GetByIdAsync(string id);
    Task<PageResponseDto> CreateAsync(PageDto dto);
    Task<PageResponseDto> UpdateAsync(string id, PageDto dto);
    Task DeleteAsync(string id);
}

public class PageService : IPageService
{
    private readonly IInformativePageRepository _repo;

    public PageService(IInformativePageRepository repo) => _repo = repo;

    public async Task<List<PageResponseDto>> GetAllAsync() =>
        PageMapper.ToDtoList(await _repo.GetAllAsync());

    public async Task<List<PageResponseDto>> GetFooterPagesAsync() =>
        PageMapper.ToDtoList(await _repo.GetFooterPagesAsync());

    public async Task<PageResponseDto?> GetBySlugAsync(string slug)
    {
        var page = await _repo.GetBySlugAsync(slug);
        return page is not null ? PageMapper.ToDto(page) : null;
    }

    public async Task<PageResponseDto?> GetByIdAsync(string id)
    {
        var page = await _repo.GetByIdAsync(id);
        return page is not null ? PageMapper.ToDto(page) : null;
    }

    public async Task<PageResponseDto> CreateAsync(PageDto dto)
    {
        var page = PageMapper.ToEntity(dto);
        page.ValidarInvariantes();

        if (await _repo.SlugExistsAsync(page.Slug))
            throw new DuplicateSlugException(page.Slug);

        await _repo.InsertAsync(page);
        return PageMapper.ToDto(page);
    }

    public async Task<PageResponseDto> UpdateAsync(string id, PageDto dto)
    {
        var page = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Sección", id);

        PageMapper.ApplyUpdate(page, dto);
        page.ValidarInvariantes();

        if (await _repo.SlugExistsAsync(page.Slug, id))
            throw new DuplicateSlugException(page.Slug);

        await _repo.UpdateAsync(page);
        return PageMapper.ToDto(page);
    }

    public async Task DeleteAsync(string id)
    {
        _ = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Sección", id);
        await _repo.DeleteAsync(id);
    }
}

// ─── Auth Service (interfaz; implementación en Infrastructure.Auth) ───────────

public interface IAuthService
{
    Task<bool> ValidateCredentialsAsync(string username, string password);
    Task<AdminUserDto?> GetUserAsync(string username);
}
