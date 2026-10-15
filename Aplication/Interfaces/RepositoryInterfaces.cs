using Domine.Entities;

namespace Aplication.Interfaces;

// ─── Category Repository Interface ───────────────────────────────────────────

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<List<Category>> GetMenuCategoriesAsync();
    Task<Category?> GetByIdAsync(string id);
    Task<Category?> GetBySlugAsync(string slug);
    Task<List<Category>> GetByParentIdAsync(string? parentId);
    Task InsertAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(string id);
    Task<bool> SlugExistsAsync(string slug, string? excludeId = null);
}

// ─── Article Repository Interface ────────────────────────────────────────────

public interface IArticleRepository
{
    Task<List<Article>> GetPublishedAsync(int skip = 0, int take = 9);
    Task<List<Article>> GetFeaturedAsync(int take = 3);
    Task<List<Article>> GetByCategoryAsync(string categoryId, int skip = 0, int take = 9);
    Task<long> CountByCategoryAsync(string categoryId);
    Task<long> CountPublishedAsync();
    Task<Article?> GetBySlugAsync(string slug);
    Task<Article?> GetByIdAsync(string id);
    Task<List<Article>> GetAllAsync(int skip = 0, int take = 50);
    Task<List<Article>> SearchAsync(string text, int skip = 0, int take = 9);
    Task<long> CountSearchAsync(string text);
    Task InsertAsync(Article article);
    Task UpdateAsync(Article article);
    Task DeleteAsync(string id);
    Task IncrementViewCountAsync(string id);
    Task<bool> SlugExistsAsync(string slug, string? excludeId = null);
    Task<List<Article>> GetRecommendedAsync(string currentArticleId, string? categoryId, int take = 3);
}

// ─── Comment Repository Interface ─────────────────────────────────────────────

public interface ICommentRepository
{
    Task<List<Comment>> GetByArticleAsync(string articleId, CommentStatus status = CommentStatus.Aprobado);
    Task<List<Comment>> GetPendingAsync();
    Task<List<Comment>> GetByStatusAsync(CommentStatus status);
    Task<List<Comment>> GetAllAsync();
    Task<Comment?> GetByIdAsync(string id);
    Task InsertAsync(Comment comment);
    Task UpdateStatusAsync(string id, CommentStatus status);
    Task DeleteAsync(string id);
}

// ─── Visitor Repository Interface ─────────────────────────────────────────────

public interface IVisitorRepository
{
    Task<Visitor?> GetByEmailAsync(string email);
    Task InsertAsync(Visitor visitor);
    Task UpdateAsync(Visitor visitor);
}

// ─── InformativePage Repository Interface ─────────────────────────────────────

public interface IInformativePageRepository
{
    Task<List<InformativePage>> GetAllAsync();
    Task<List<InformativePage>> GetFooterPagesAsync();
    Task<InformativePage?> GetBySlugAsync(string slug);
    Task<InformativePage?> GetByIdAsync(string id);
    Task InsertAsync(InformativePage page);
    Task UpdateAsync(InformativePage page);
    Task DeleteAsync(string id);
}

// ─── ImageMetadata Repository Interface ───────────────────────────────────────

public interface IImageMetadataRepository
{
    Task<List<ImageMetadata>> GetAllAsync(int skip = 0, int take = 50);
    Task<List<ImageMetadata>> GetByArticleIdAsync(string articleId);
    Task<ImageMetadata?> GetByIdAsync(string id);
    Task InsertAsync(ImageMetadata image);
    Task UpdateAltAsync(string id, string alt);
    Task UpdateArticleIdAsync(string id, string? articleId);
    Task DeleteAsync(string id);
}

// ─── AdminUser Repository Interface ───────────────────────────────────────────

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username);
    Task<AdminUser?> GetByIdAsync(string id);
    Task InsertAsync(AdminUser user);
    Task UpdateLastLoginAsync(string id);
}
