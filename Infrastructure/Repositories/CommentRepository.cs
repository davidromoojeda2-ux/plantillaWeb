using Aplication.Interfaces;
using Domine.Entities;
using Infrastructure.Persistence;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly IMongoCollection<Comment> _col;

    public CommentRepository(MongoDbContext ctx) => _col = ctx.Comments;

    public async Task<List<Comment>> GetByArticleAsync(string articleId, CommentStatus status = CommentStatus.Aprobado) =>
        await _col.Find(c => c.ArticleId == articleId && c.Status == status).SortBy(c => c.CreatedAt).ToListAsync();

    public async Task<List<Comment>> GetPendingAsync() =>
        await _col.Find(c => c.Status == CommentStatus.Pendiente).SortBy(c => c.CreatedAt).ToListAsync();

    public async Task<List<Comment>> GetByStatusAsync(CommentStatus status) =>
        await _col.Find(c => c.Status == status).SortByDescending(c => c.CreatedAt).ToListAsync();

    public async Task<List<Comment>> GetAllAsync() =>
        await _col.Find(_ => true).SortByDescending(c => c.CreatedAt).ToListAsync();

    public async Task<Comment?> GetByIdAsync(string id) =>
        await _col.Find(c => c.Id == id).FirstOrDefaultAsync();

    public async Task InsertAsync(Comment comment) => await _col.InsertOneAsync(comment);

    public async Task UpdateStatusAsync(string id, CommentStatus status) =>
        await _col.UpdateOneAsync(c => c.Id == id,
            Builders<Comment>.Update.Set(c => c.Status, status));

    public async Task DeleteAsync(string id) => await _col.DeleteOneAsync(c => c.Id == id);
}

public class VisitorRepository : IVisitorRepository
{
    private readonly IMongoCollection<Visitor> _col;

    public VisitorRepository(MongoDbContext ctx) => _col = ctx.Visitors;

    public async Task<Visitor?> GetByEmailAsync(string email) =>
        await _col.Find(v => v.Email == email).FirstOrDefaultAsync();

    public async Task InsertAsync(Visitor visitor) => await _col.InsertOneAsync(visitor);

    public async Task UpdateAsync(Visitor visitor) =>
        await _col.ReplaceOneAsync(v => v.Id == visitor.Id, visitor);
}
