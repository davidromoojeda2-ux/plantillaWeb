using MongoDB.Bson;
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
        // El índice de texto anterior indexaba el HTML libre. Se elimina de forma
        // defensiva para que la búsqueda use únicamente SearchText.
        try
        {
            await context.Articles.Indexes.DropOneAsync("tx_articles_fulltext");
        }
        catch (MongoCommandException ex) when (ex.Code == 27)
        {
            // IndexNotFound: la base de datos es nueva o ya fue migrada.
        }

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
                Builders<Domine.Entities.Article>.IndexKeys.Text(a => a.SearchText),
                new CreateIndexOptions { Name = "tx_articles_search" }),

            new CreateIndexModel<Domine.Entities.Article>(
                Builders<Domine.Entities.Article>.IndexKeys
                    .Ascending(a => a.DocumentIds)
                    .Ascending(a => a.Status),
                new CreateIndexOptions { Name = "ix_articles_documents_status" })
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

        // ─── images ───────────────────────────────────────────────────────────
        await context.Images.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Domine.Entities.ImageMetadata>(
                Builders<Domine.Entities.ImageMetadata>.IndexKeys.Ascending(i => i.ArticleId),
                new CreateIndexOptions { Name = "ix_images_article_id" })
        ]);

        // ─── documents ────────────────────────────────────────────────────────
        await EnsureCollectionExistsAsync(context.Database, "documents");
        await context.Documents.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Domine.Entities.DocumentMetadata>(
                Builders<Domine.Entities.DocumentMetadata>.IndexKeys.Descending(d => d.UploadedAt),
                new CreateIndexOptions { Name = "ix_documents_uploaded" }),

            new CreateIndexModel<Domine.Entities.DocumentMetadata>(
                Builders<Domine.Entities.DocumentMetadata>.IndexKeys.Ascending(d => d.IsPublic),
                new CreateIndexOptions { Name = "ix_documents_public" }),

            new CreateIndexModel<Domine.Entities.DocumentMetadata>(
                Builders<Domine.Entities.DocumentMetadata>.IndexKeys.Ascending(d => d.Storage),
                new CreateIndexOptions { Name = "ix_documents_storage" })
        ]);

        // ─── automation_runs ──────────────────────────────────────────────────
        await EnsureCollectionExistsAsync(context.Database, "automation_runs");
        await context.AutomationRuns.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Domine.Entities.AutomationRun>(
                Builders<Domine.Entities.AutomationRun>.IndexKeys.Descending(r => r.CreatedAt),
                new CreateIndexOptions { Name = "ix_automation_runs_created" }),

            new CreateIndexModel<Domine.Entities.AutomationRun>(
                Builders<Domine.Entities.AutomationRun>.IndexKeys.Ascending(r => r.TopicHashes),
                new CreateIndexOptions { Name = "ix_automation_runs_hashes" })
        ]);

        // ─── automation_settings (documento único) ─────────────────────────────
        await EnsureCollectionExistsAsync(context.Database, "automation_settings");
    }

    private static async Task EnsureCollectionExistsAsync(IMongoDatabase database, string name)
    {
        var names = await database.ListCollectionNamesAsync();
        var list = await names.ToListAsync();
        if (!list.Contains(name))
            await database.CreateCollectionAsync(name);
    }

    public static async Task EnsureSeedDataAsync(MongoDbContext context)
    {
        // ─── Usuario Admin Inicial ─────────────────────────────────────────────
        var adminCount = await context.Users.CountDocumentsAsync(Builders<Domine.Entities.AdminUser>.Filter.Empty);
        if (adminCount == 0)
        {
            var adminUser = new Domine.Entities.AdminUser
            {
                Username = "admin",
                Email = "admin@portalnoticias.com",
                DisplayName = "Administrador del Sistema",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await context.Users.InsertOneAsync(adminUser);
        }

        // ─── Páginas informativas ──────────────────────────────────────────────
        // La sección institucional se siembra manualmente con
        // InstitutionalPagesSeed, no de forma automática al arrancar.

        // ─── Auto-vincular artículos huérfanos sin categoría (CategoryId == null) ───
        await EnsureOrphanArticlesLinkedAsync(context);
    }

    /// <summary>
    /// Migra las páginas informativas guardadas antes del refactor a bloques estructurados.
    /// Si el documento conserva el campo legado <c>Content</c> (HTML) y todavía no tiene
    /// <c>Blocks</c>, convierte el HTML a bloques sanitizados; en cualquier caso elimina el
    /// campo obsoleto. Es idempotente: los documentos ya migrados no se vuelven a tocar.
    /// </summary>
    /// <returns>Número de documentos procesados.</returns>
    public static async Task<int> MigrateLegacyPageContentAsync(MongoDbContext context)
    {
        var rawPages = context.Database.GetCollection<BsonDocument>("pages");
        var legacyFilter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Exists("Content"),
            Builders<BsonDocument>.Filter.Ne("Content", BsonNull.Value));

        var legacyPages = await rawPages.Find(legacyFilter).ToListAsync();
        if (legacyPages.Count == 0) return 0;

        var migrated = 0;
        foreach (var document in legacyPages)
        {
            var rawId = document["_id"];
            var id = rawId.IsObjectId ? rawId.AsObjectId.ToString() : rawId.ToString();

            var page = await context.Pages.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (page is null) continue;

            var update = Builders<Domine.Entities.InformativePage>.Update
                .Set(p => p.UpdatedAt, DateTime.UtcNow)
                .Unset("Content");

            if (page.Blocks.Count == 0)
            {
                var html = document["Content"].IsString ? document["Content"].AsString : string.Empty;
                var blocks = LegacyContentBlocks.FromHtml(html);

                if (blocks.Count > 0)
                {
                    update = update
                        .Set(p => p.Blocks, blocks)
                        .Set(p => p.SchemaVersion, Domine.Content.ContentLimits.CurrentSchemaVersion);
                }
            }

            await context.Pages.UpdateOneAsync(p => p.Id == id, update);
            migrated++;
        }

        return migrated;
    }

    public static async Task EnsureOrphanArticlesLinkedAsync(MongoDbContext context)
    {
        // Filtro seguro para ObjectId opcional
        var orphanFilter = Builders<Domine.Entities.Article>.Filter.Eq(a => a.CategoryId, null);

        var orphanArticles = await context.Articles.Find(orphanFilter).ToListAsync();
        if (!orphanArticles.Any()) return;

        var allCategories = await context.Categories.Find(_ => true).ToListAsync();
        if (!allCategories.Any()) return;

        var defaultRootCategory = allCategories.FirstOrDefault(c => c.ParentId == null && c.IsActive)
                               ?? allCategories.FirstOrDefault(c => c.ParentId == null)
                               ?? allCategories.First();

        foreach (var art in orphanArticles)
        {
            Domine.Entities.Category? matchedCategory = null;
            Domine.Entities.Category? matchedSubcategory = null;

            // Vinculación dinámica agnóstica: compara los Tags y título del artículo con las categorías existentes en BD
            foreach (var cat in allCategories)
            {
                var matchesTag = art.Tags.Any(t =>
                    string.Equals(t.Trim(), cat.Name.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Trim(), cat.Slug.Trim(), StringComparison.OrdinalIgnoreCase));

                if (matchesTag)
                {
                    if (cat.ParentId is not null)
                    {
                        matchedSubcategory = cat;
                        matchedCategory = allCategories.FirstOrDefault(c => c.Id == cat.ParentId);
                    }
                    else
                    {
                        matchedCategory = cat;
                    }
                    break;
                }
            }

            var chosenCategory = matchedCategory ?? defaultRootCategory;
            string categoryId;
            string? subcategoryId;

            if (chosenCategory.ParentId is not null)
            {
                categoryId = chosenCategory.ParentId;
                subcategoryId = chosenCategory.Id;
            }
            else
            {
                categoryId = chosenCategory.Id;
                subcategoryId = matchedSubcategory?.Id;
            }

            var updateDef = Builders<Domine.Entities.Article>.Update
                .Set(a => a.CategoryId, categoryId)
                .Set(a => a.SubcategoryId, subcategoryId)
                .Set(a => a.UpdatedAt, DateTime.UtcNow);

            await context.Articles.UpdateOneAsync(a => a.Id == art.Id, updateDef);
        }
    }
}
