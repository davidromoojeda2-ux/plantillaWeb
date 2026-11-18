using System.Text.RegularExpressions;
using Aplication.Configuration;
using Aplication.Interfaces;
using Domine.Entities;
using Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

/// <summary>
/// Persistencia de documentos y consultas de la biblioteca del CMS. La
/// asociación con artículos vive en <see cref="Article.DocumentIds"/>; este
/// repositorio la usa para filtrar por artículo sin duplicar la relación.
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly IMongoCollection<DocumentMetadata> _col;
    private readonly IMongoCollection<Article> _articles;

    public DocumentRepository(MongoDbContext ctx)
    {
        _col = ctx.Documents;
        _articles = ctx.Articles;
    }

    public async Task<List<DocumentMetadata>> GetAllAsync(int skip = 0, int take = 50) =>
        await _col.Find(_ => true)
            .SortByDescending(d => d.UploadedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();

    public async Task<List<DocumentMetadata>> GetByIdsAsync(IEnumerable<string> ids)
    {
        var idList = ids
            .Where(id => !string.IsNullOrWhiteSpace(id) && ObjectId.TryParse(id, out _))
            .Distinct()
            .ToList();
        if (idList.Count == 0) return [];

        return await _col.Find(d => idList.Contains(d.Id)).ToListAsync();
    }

    public async Task<DocumentMetadata?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out _))
            return null;

        return await _col.Find(d => d.Id == id).FirstOrDefaultAsync();
    }

    public async Task<long> CountAsync(
        string? search = null,
        string? storage = null,
        string? extension = null,
        string? articleFilter = null,
        CancellationToken ct = default)
    {
        var filter = await BuildFilterAsync(search, storage, extension, articleFilter, ct);
        return await _col.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    public async Task<List<DocumentMetadata>> GetPagedAsync(
        int skip,
        int take,
        string? search = null,
        string? storage = null,
        string? extension = null,
        string? articleFilter = null,
        CancellationToken ct = default)
    {
        var filter = await BuildFilterAsync(search, storage, extension, articleFilter, ct);
        return await _col.Find(filter)
            .SortByDescending(d => d.UploadedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(ct);
    }

    public async Task InsertAsync(DocumentMetadata document) =>
        await _col.InsertOneAsync(document);

    public async Task UpdateMetadataAsync(string id, string displayName, string? description, bool isPublic) =>
        await _col.UpdateOneAsync(d => d.Id == id,
            Builders<DocumentMetadata>.Update
                .Set(d => d.DisplayName, displayName)
                .Set(d => d.Description, description)
                .Set(d => d.IsPublic, isPublic));

    public async Task IncrementDownloadCountAsync(string id) =>
        await _col.UpdateOneAsync(d => d.Id == id,
            Builders<DocumentMetadata>.Update.Inc(d => d.DownloadCount, 1));

    public async Task DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out _))
            return;

        await _col.DeleteOneAsync(d => d.Id == id);
    }

    private async Task<FilterDefinition<DocumentMetadata>> BuildFilterAsync(
        string? search,
        string? storage,
        string? extension,
        string? articleFilter,
        CancellationToken ct)
    {
        var builder = Builders<DocumentMetadata>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var regex = new BsonRegularExpression(Regex.Escape(search.Trim()), "i");
            filter &= builder.Regex(d => d.DisplayName, regex) |
                      builder.Regex(d => d.OriginalFileName, regex) |
                      builder.Regex(d => d.Description, regex);
        }

        if (!string.IsNullOrWhiteSpace(storage))
        {
            var normalized = storage.Trim().ToLowerInvariant();
            if (normalized is "s3" or "r2")
            {
                filter &= builder.Eq(d => d.Storage, "s3") |
                          builder.Eq(d => d.Storage, "r2");
            }
            else
            {
                filter &= builder.Eq(d => d.Storage, normalized);
            }
        }

        if (!string.IsNullOrWhiteSpace(extension))
        {
            var normalizedExtension = DocumentsOptions.NormalizeExtension(extension);
            filter &= builder.Eq(d => d.Extension, normalizedExtension);
        }

        if (!string.IsNullOrWhiteSpace(articleFilter))
        {
            var normalizedArticle = articleFilter.Trim();
            if (normalizedArticle.Equals("linked", StringComparison.OrdinalIgnoreCase))
            {
                var linkedIds = await GetLinkedDocumentIdsAsync(ct);
                filter &= builder.In(d => d.Id, linkedIds);
            }
            else if (normalizedArticle.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                var linkedIds = await GetLinkedDocumentIdsAsync(ct);
                filter &= builder.Nin(d => d.Id, linkedIds);
            }
            else
            {
                var articleDocumentIds = await _articles
                    .Find(a => a.Id == normalizedArticle)
                    .Project(a => a.DocumentIds)
                    .FirstOrDefaultAsync(ct) ?? [];

                filter &= builder.In(d => d.Id, articleDocumentIds);
            }
        }

        return filter;
    }

    private async Task<List<string>> GetLinkedDocumentIdsAsync(CancellationToken ct)
    {
        var documentIdLists = await _articles
            .Find(_ => true)
            .Project(a => a.DocumentIds)
            .ToListAsync(ct);

        return documentIdLists
            .Where(ids => ids is not null)
            .SelectMany(ids => ids)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
    }
}
