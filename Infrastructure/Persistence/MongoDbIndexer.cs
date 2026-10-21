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

        // ─── images ───────────────────────────────────────────────────────────
        await context.Images.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Domine.Entities.ImageMetadata>(
                Builders<Domine.Entities.ImageMetadata>.IndexKeys.Ascending(i => i.ArticleId),
                new CreateIndexOptions { Name = "ix_images_article_id" })
        ]);
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

        // ─── Páginas Informativas Básicas ─────────────────────────────────────
        var pageCount = await context.Pages.CountDocumentsAsync(Builders<Domine.Entities.InformativePage>.Filter.Empty);
        if (pageCount == 0)
        {
            var politicas = new Domine.Entities.InformativePage
            {
                Title = "Políticas de Calidad y Ética Editorial",
                Slug = "politicas-de-calidad",
                Content = "<h2>1. Compromiso con la Veracidad</h2><p>Nuestro portal se rige por principios de rigor informativo, verificación exhaustiva de hechos y separación clara entre información y opinión.</p><h2>2. Independencia Editorial</h2><p>Las decisiones editoriales son completamente autónomas e independientes.</p>",
                Order = 1,
                ShowInFooter = true
            };
            var privacidad = new Domine.Entities.InformativePage
            {
                Title = "Política de Privacidad y Cookies",
                Slug = "politica-de-privacidad",
                Content = "<h2>Tratamiento de Datos Personales</h2><p>Respetamos tu privacidad. No almacenamos direcciones IP en texto plano ni comercializamos datos personales con terceros.</p>",
                Order = 2,
                ShowInFooter = true
            };
            await context.Pages.InsertManyAsync([politicas, privacidad]);
        }

        // ─── Siembra de Artículos de Prueba (Mínimo 10 completos de temas variados) ───
        await SeedArticlesData.SeedAsync(context);

        // ─── Auto-vincular artículos huérfanos sin categoría (CategoryId == null) ───
        await EnsureOrphanArticlesLinkedAsync(context);
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
