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

        // ─── Categorías Iniciales ──────────────────────────────────────────────
        var catCount = await context.Categories.CountDocumentsAsync(Builders<Domine.Entities.Category>.Filter.Empty);
        if (catCount == 0)
        {
            var tecnologia = new Domine.Entities.Category
            {
                Name = "Tecnología",
                Slug = "tecnologia",
                Description = "Noticias de innovación, software y gadgets",
                Order = 1,
                ShowInMenu = true,
                IsActive = true
            };
            var economia = new Domine.Entities.Category
            {
                Name = "Economía",
                Slug = "economia",
                Description = "Finanzas, mercados y negocios globales",
                Order = 2,
                ShowInMenu = true,
                IsActive = true
            };
            var cultura = new Domine.Entities.Category
            {
                Name = "Cultura",
                Slug = "cultura",
                Description = "Arte, cine, literatura y sociedad",
                Order = 3,
                ShowInMenu = true,
                IsActive = true
            };

            await context.Categories.InsertManyAsync([tecnologia, economia, cultura]);

            var iaSub = new Domine.Entities.Category
            {
                Name = "Inteligencia Artificial",
                Slug = "inteligencia-artificial",
                ParentId = tecnologia.Id,
                Order = 1,
                ShowInMenu = true,
                IsActive = true
            };
            var ciberSub = new Domine.Entities.Category
            {
                Name = "Ciberseguridad",
                Slug = "ciberseguridad",
                ParentId = tecnologia.Id,
                Order = 2,
                ShowInMenu = true,
                IsActive = true
            };
            await context.Categories.InsertManyAsync([iaSub, ciberSub]);
        }

        // ─── Páginas Informativas Iniciales ───────────────────────────────────
        var pageCount = await context.Pages.CountDocumentsAsync(Builders<Domine.Entities.InformativePage>.Filter.Empty);
        if (pageCount == 0)
        {
            var politicas = new Domine.Entities.InformativePage
            {
                Title = "Políticas de Calidad y Ética Editorial",
                Slug = "politicas-de-calidad",
                Content = "<h2>1. Compromiso con la Veracidad</h2><p>Nuestro portal se rige por principios de rigor periodístico, verificación exhaustiva de hechos y separación clara entre información y opinión.</p><h2>2. Independencia Editorial</h2><p>Las decisiones editoriales son completamente autónomas e inmunes a presiones comerciales o partidarias.</p>",
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

        // ─── Artículos Iniciales ──────────────────────────────────────────────
        var articleCount = await context.Articles.CountDocumentsAsync(Builders<Domine.Entities.Article>.Filter.Empty);
        if (articleCount == 0)
        {
            var catTec = await context.Categories.Find(c => c.Slug == "tecnologia").FirstOrDefaultAsync();
            var catEco = await context.Categories.Find(c => c.Slug == "economia").FirstOrDefaultAsync();
            var catCul = await context.Categories.Find(c => c.Slug == "cultura").FirstOrDefaultAsync();

            var articles = new List<Domine.Entities.Article>
            {
                new Domine.Entities.Article
                {
                    Title = "Revolución de la Inteligencia Artificial y la Nueva Era de la Automatización",
                    Slug = "revolucion-inteligencia-artificial-nueva-era",
                    Summary = "Un análisis exhaustivo sobre cómo los nuevos modelos generativos y la computación en la nube transforman la industria global.",
                    Content = "<h2>1. El Estado Actual del Ecosistema Tecnológico</h2><p>El ritmo de avance en modelos fundacionales ha sobrepasado las estimaciones iniciales. Las organizaciones aceleran su migración hacia arquitecturas híbridas y seguras.</p><h2>2. Desafíos y Gobernanza de Datos</h2><p>La seguridad de la infraestructura y el cumplimiento regulatorio son pilares críticos para garantizar operaciones confiables a escala.</p>",
                    Author = "Redacción de Tecnología",
                    CategoryId = catTec?.Id,
                    Tags = ["Tecnología", "IA", "Innovación"],
                    Featured = true,
                    Status = Domine.Entities.ArticleStatus.Publicado,
                    PublishedAt = DateTime.UtcNow.AddHours(-2),
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    UpdatedAt = DateTime.UtcNow.AddHours(-2),
                    ReadingTimeMinutes = 4,
                    ViewCount = 145
                },
                new Domine.Entities.Article
                {
                    Title = "Mercados Globales y Nuevas Perspectivas Financieras para este Trimestre",
                    Slug = "mercados-globales-perspectivas-financieras",
                    Summary = "Evaluación macroeconómica sobre las tasas de interés, comercio internacional y flujo de inversiones sostenibles.",
                    Content = "<h2>Comercio y Política Monetaria</h2><p>Los bancos centrales ajustan sus políticas ante la desaceleración de la inflación, impulsando la confianza en mercados emergentes.</p>",
                    Author = "Análisis Económico",
                    CategoryId = catEco?.Id,
                    Tags = ["Economía", "Finanzas", "Mercados"],
                    Featured = false,
                    Status = Domine.Entities.ArticleStatus.Publicado,
                    PublishedAt = DateTime.UtcNow.AddHours(-5),
                    CreatedAt = DateTime.UtcNow.AddHours(-5),
                    UpdatedAt = DateTime.UtcNow.AddHours(-5),
                    ReadingTimeMinutes = 3,
                    ViewCount = 89
                },
                new Domine.Entities.Article
                {
                    Title = "Preservación del Patrimonio y Nuevos Espacios Culturales Contemporáneos",
                    Slug = "preservacion-patrimonio-espacios-culturales",
                    Summary = "Iniciativas comunitarias e institucionales rescatan centros históricos y fomentan el diálogo artístico multidisciplinario.",
                    Content = "<h2>Encuentro entre Historia y Modernidad</h2><p>La integración de técnicas digitales permite documentar y difundir expresiones culturales en formatos interactivos para nuevas generaciones.</p>",
                    Author = "Redacción Cultural",
                    CategoryId = catCul?.Id,
                    Tags = ["Cultura", "Arte", "Sociedad"],
                    Featured = false,
                    Status = Domine.Entities.ArticleStatus.Publicado,
                    PublishedAt = DateTime.UtcNow.AddHours(-8),
                    CreatedAt = DateTime.UtcNow.AddHours(-8),
                    UpdatedAt = DateTime.UtcNow.AddHours(-8),
                    ReadingTimeMinutes = 3,
                    ViewCount = 62
                }
            };

            await context.Articles.InsertManyAsync(articles);
        }
    }
}
