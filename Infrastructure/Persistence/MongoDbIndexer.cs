using Infrastructure.Persistence;
using MongoDB.Driver;

namespace Infrastructure.Persistence;

/// <summary>
/// Inicializa todos los índices requeridos en MongoDB al arrancar la aplicación.
/// Llamar desde el startup después de construir el host.
/// </summary>
public static class MongoDbIndexer
{
    public static async Task EnsureIndexesAsync(MongoDbContext context)
    {
        // ─── categories ───────────────────────────────────────────────────────
        await context.Categories.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Domine.Entities.Category>(
                Builders<Domine.Entities.Category>.IndexKeys.Ascending(c => c.Slug),
                new CreateIndexOptions { Unique = true, Name = "ux_categories_slug" }),

            new CreateIndexModel<Domine.Entities.Category>(
                Builders<Domine.Entities.Category>.IndexKeys
                    .Ascending(c => c.ParentId)
                    .Ascending(c => c.Order),
                new CreateIndexOptions { Name = "ix_categories_parent_order" })
        ]);

        // ─── articles ─────────────────────────────────────────────────────────
        await context.Articles.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Domine.Entities.Article>(
                Builders<Domine.Entities.Article>.IndexKeys.Ascending(a => a.Slug),
                new CreateIndexOptions { Unique = true, Name = "ux_articles_slug" }),

            new CreateIndexModel<Domine.Entities.Article>(
                Builders<Domine.Entities.Article>.IndexKeys
                    .Ascending(a => a.Status)
                    .Descending(a => a.PublishedAt),
                new CreateIndexOptions { Name = "ix_articles_status_published" }),

            new CreateIndexModel<Domine.Entities.Article>(
                Builders<Domine.Entities.Article>.IndexKeys
                    .Ascending(a => a.CategoryId)
                    .Descending(a => a.PublishedAt),
                new CreateIndexOptions { Name = "ix_articles_category_published" }),

            new CreateIndexModel<Domine.Entities.Article>(
                Builders<Domine.Entities.Article>.IndexKeys.Text(a => a.Title).Text(a => a.Summary).Text(a => a.Content),
                new CreateIndexOptions { Name = "tx_articles_fulltext" })
        ]);

        // ─── comments ─────────────────────────────────────────────────────────
        await context.Comments.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Domine.Entities.Comment>(
                Builders<Domine.Entities.Comment>.IndexKeys
                    .Ascending(c => c.ArticleId)
                    .Ascending(c => c.Status),
                new CreateIndexOptions { Name = "ix_comments_article_status" })
        ]);

        // ─── visitors ─────────────────────────────────────────────────────────
        await context.Visitors.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Domine.Entities.Visitor>(
                Builders<Domine.Entities.Visitor>.IndexKeys.Ascending(v => v.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_visitors_email" })
        ]);

        // ─── pages ────────────────────────────────────────────────────────────
        await context.Pages.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Domine.Entities.InformativePage>(
                Builders<Domine.Entities.InformativePage>.IndexKeys.Ascending(p => p.Slug),
                new CreateIndexOptions { Unique = true, Name = "ux_pages_slug" })
        ]);

        // ─── users ────────────────────────────────────────────────────────────
        await context.Users.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Domine.Entities.AdminUser>(
                Builders<Domine.Entities.AdminUser>.IndexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Unique = true, Name = "ux_users_username" }),

            new CreateIndexModel<Domine.Entities.AdminUser>(
                Builders<Domine.Entities.AdminUser>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_users_email" })
        ]);
    }
}
