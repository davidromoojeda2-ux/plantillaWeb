using Aplication.DTOs;
using Aplication.Mappers;

namespace paginaWeb.Models.Home;

// ─── View models de presentación de la Home ──────────────────────────────────
// Son exclusivos de la capa de vista: no alteran DTOs, servicios ni la API.

/// <summary>Imagen de tarjeta con dimensiones explícitas para evitar saltos de layout (CLS).</summary>
public sealed record CardImage(string? Url, string Alt, int Width, int Height)
{
    public bool HasUrl => !string.IsNullOrWhiteSpace(Url);
}

/// <summary>Tarjeta del hero que se superpone a la curva.</summary>
public sealed record FeatureCardViewModel(
    string Title,
    string Text,
    string Category,
    string Href,
    CardImage Image,
    DateTime? PublishedAt = null,
    string? Author = null,
    int ReadingMinutes = 3);

/// <summary>Tarjeta de la cuadrícula "Últimas publicaciones" y catálogo general de artículos.</summary>
public sealed record PostCardViewModel(
    string Title,
    string Excerpt,
    string Category,
    DateTime? PublishedAt,
    string Href,
    CardImage Image,
    bool AsGridItem = true,
    string? CustomClass = null,
    long ViewCount = 0,
    int? Rank = null)
{
    public static PostCardViewModel FromArticle(
        paginaWeb.Models.Category.ArticleCardViewModel item,
        string? href = null,
        bool asGridItem = true)
    {
        var slug = item.Article?.Slug ?? string.Empty;
        var finalHref = !string.IsNullOrWhiteSpace(href) ? href : $"/articulos/{slug}";
        var title = item.Article?.Title ?? "Publicación";
        var excerpt = item.Article?.Summary ?? string.Empty;
        var category = !string.IsNullOrWhiteSpace(item.CategoryName) ? item.CategoryName : "Actualidad";
        var imageUrl = !string.IsNullOrWhiteSpace(item.ImageUrl) ? item.ImageUrl : item.Article?.CoverImageUrl;

        return new PostCardViewModel(
            Title: title,
            Excerpt: excerpt,
            Category: category,
            PublishedAt: item.Article?.PublishedAt,
            Href: finalHref,
            Image: new CardImage(
                Url: imageUrl,
                Alt: title,
                Width: 800,
                Height: 500),
            AsGridItem: asGridItem
        );
    }

    public static PostCardViewModel FromDto(
        Aplication.DTOs.ArticleResponseDto? article,
        string? categoryName = null,
        string? imageUrl = null,
        string? href = null,
        bool asGridItem = true)
    {
        var slug = article?.Slug ?? string.Empty;
        var finalHref = !string.IsNullOrWhiteSpace(href) ? href : $"/articulos/{slug}";
        var title = article?.Title ?? "Publicación";
        var excerpt = article?.Summary ?? string.Empty;
        var category = !string.IsNullOrWhiteSpace(categoryName) ? categoryName : "Actualidad";
        var finalImageUrl = !string.IsNullOrWhiteSpace(imageUrl) ? imageUrl : article?.CoverImageUrl;

        return new PostCardViewModel(
            Title: title,
            Excerpt: excerpt,
            Category: category,
            PublishedAt: article?.PublishedAt,
            Href: finalHref,
            Image: new CardImage(
                Url: finalImageUrl,
                Alt: title,
                Width: 800,
                Height: 500),
            AsGridItem: asGridItem,
            ViewCount: article?.ViewCount ?? 0
        );
    }
}

/// <summary>Encabezado h2 de sección con enlace opcional con flecha.</summary>
public sealed record SectionHeaderViewModel(
    string HeadingId,
    string Title,
    string? Subtitle = null,
    string? LinkText = null,
    string? LinkHref = null);

/// <summary>Datos del hero (h1, subtítulo y tarjetas).</summary>
public sealed record HeroViewModel(
    string Title,
    string Subtitle,
    IReadOnlyList<FeatureCardViewModel> Cards);

/// <summary>Tarjeta temática del mosaico Bento para la sección de categorías.</summary>
public sealed record CategoryBentoCardViewModel(
    string Name,
    string Slug,
    string? Description,
    bool IsMainCategory,
    string? ParentName,
    string? ParentSlug,
    IReadOnlyList<CategoryResponseDto> Subcategories,
    string? ImageUrl = null);

/// <summary>Sección 'Encuentra el tema de tu interés' estructurada en formato Bento de 2 columnas.</summary>
public sealed record CategorySectionViewModel(
    string Title,
    string Subtitle,
    CategoryBentoCardViewModel? MainCard,
    CategoryBentoCardViewModel? SubCardLeft,
    CategoryBentoCardViewModel? SubCardRightTop,
    CategoryBentoCardViewModel? SubCardRightBottom,
    IReadOnlyList<CategoryResponseDto> AllCategories);

/// <summary>
/// Sección editorial persuasiva con métricas en tiempo real del catálogo (artículos y categorías).
/// </summary>
public sealed record textPersuasivSectionViewModel(
    string Kicker,
    string Title,
    string HighlightText,
    string Subtitle,
    long TotalArticles,
    int TotalCategories,
    int TotalSubcategories,
    string? ExploreHref = null
);

/// <summary>Modelo completo de la página principal.</summary>
public sealed record HomePageViewModel(
    HeroViewModel Hero,
    SectionHeaderViewModel EntriesHeader,
    IReadOnlyList<PostCardViewModel> LatestEntries,
    IReadOnlyList<PostCardViewModel> MostReadEntries,
    CategorySectionViewModel CategorySection,
    textPersuasivSectionViewModel TextoPersuasivo);

/// <summary>
/// Construye el <see cref="HomePageViewModel"/> a partir de los datos entregados por
/// los servicios de la aplicación en <c>HomeController.Index</c>.
/// </summary>
public static class HomePageViewModelFactory
{
    public const int ColumnEntryCount = 3;
    public const int TopReadEntryCount = 5;

    private const int CardImageWidth = 800;
    private const int CardImageHeight = 500; // relación 16:10

    public static HomePageViewModel Create(
        IReadOnlyList<ArticleResponseDto> recent,
        IReadOnlyList<ArticleResponseDto> featured,
        IReadOnlyList<CategoryResponseDto> allCategories,
        IReadOnlyDictionary<string, ImageMetadataResponseDto> images,
        Func<string, string> articleHref,
        string? searchHref = null,
        IReadOnlyList<ArticleResponseDto>? mostRead = null,
        long? totalArticleCount = null)
    {
        var catDict = allCategories.ToDictionary(c => c.Id, c => c.Name);

        // 1. Hero
        var heroSource = (featured.Count > 0 ? featured : recent).Take(5).ToList();
        var hero = new HeroViewModel(
            Title: "TITULO DE LA PAGINA WEB",
            Subtitle: "Noticias verificadas, análisis en profundidad y coberturas especiales de actualidad.",
            Cards: heroSource.Select(a => new FeatureCardViewModel(
                a.Title,
                a.Summary,
                CategoryName(a, catDict, "Especial"),
                articleHref(a.Slug),
                ResolveImage(a, images),
                a.PublishedAt,
                a.Author,
                a.ReadingTimeMinutes > 0 ? a.ReadingTimeMinutes : EstimateReadingMinutes(a.Summary))).ToList());

        // 2. Últimas entradas en dos columnas (3 publicaciones recientes con tarjetas grandes a la izquierda y 5 más leídas a la derecha)
        var latestEntriesSource = recent.Concat(featured)
            .DistinctBy(a => a.Id)
            .Take(ColumnEntryCount)
            .ToList();

        var mostReadList = (mostRead != null && mostRead.Count > 0)
            ? mostRead.ToList()
            : recent.OrderByDescending(a => a.ViewCount).ThenByDescending(a => a.PublishedAt).ToList();

        var mostReadEntriesSource = mostReadList.Take(TopReadEntryCount).ToList();
        if (mostReadEntriesSource.Count < TopReadEntryCount)
        {
            var missing = TopReadEntryCount - mostReadEntriesSource.Count;
            var additional = recent.Where(r => !mostReadEntriesSource.Any(m => m.Id == r.Id)).Take(missing);
            mostReadEntriesSource.AddRange(additional);
        }

        var latestEntries = latestEntriesSource.Select(a => new PostCardViewModel(
            a.Title,
            a.Summary,
            CategoryName(a, catDict, "Reciente"),
            a.PublishedAt,
            articleHref(a.Slug),
            ResolveImage(a, images),
            ViewCount: a.ViewCount)).ToList();

        var mostReadEntries = mostReadEntriesSource.Select((a, idx) => new PostCardViewModel(
            a.Title,
            a.Summary,
            CategoryName(a, catDict, "Popular"),
            a.PublishedAt,
            articleHref(a.Slug),
            ResolveImage(a, images),
            ViewCount: a.ViewCount,
            Rank: idx + 1)).ToList();

        var entriesHeader = new SectionHeaderViewModel(
            HeadingId: "ultimas-entradas-title",
            Title: "Últimas entradas",
            Subtitle: "Las publicaciones más recientes y los artículos más leídos de nuestra redacción",
            LinkText: "Ver todas",
            LinkHref: searchHref);

        // 3. Sección Bento: 'Encuentra el tema de tu interés'
        var allArticles = recent.Concat(featured).DistinctBy(a => a.Id).ToList();

        var mainCategories = allCategories
            .Where(c => string.IsNullOrEmpty(c.ParentId) && c.IsActive)
            .OrderBy(c => c.Order)
            .ToList();

        // ── Helpers de métricas derivadas de artículos ──────────────────────────
        long CatViewCount(string catId) => allArticles
            .Where(a => a.CategoryId == catId || a.SubcategoryId == catId)
            .Sum(a => a.ViewCount);

        int CatArticleCount(string catId) => allArticles
            .Count(a => a.CategoryId == catId || a.SubcategoryId == catId);

        // ── Conjunto de IDs ya asignados (sin duplicados entre cards) ────────────
        var usedCategoryIds = new HashSet<string>();

        CategoryBentoCardViewModel? BuildCard(
            CategoryResponseDto? cat,
            bool isMain,
            IReadOnlyList<CategoryResponseDto> subs,
            string fallbackDesc)
        {
            if (cat == null || string.IsNullOrEmpty(cat.Id)) return null;
            if (!usedCategoryIds.Add(cat.Id)) return null; // ya usado → no mostrar
            return new CategoryBentoCardViewModel(
                Name: cat.Name,
                Slug: cat.Slug,
                Description: cat.Description ?? fallbackDesc,
                IsMainCategory: isMain,
                ParentName: isMain ? null : allCategories.FirstOrDefault(p => p.Id == cat.ParentId)?.Name,
                ParentSlug: isMain ? null : allCategories.FirstOrDefault(p => p.Id == cat.ParentId)?.Slug,
                Subcategories: subs,
                ImageUrl: GetRandomImageForCategory(cat, allArticles, images, allCategories));
        }

        var allSubs = allCategories
            .Where(c => !string.IsNullOrEmpty(c.ParentId) && c.IsActive)
            .ToList();

        // ── SLOT MainCard (top-left grande): categoría principal con MÁS ARTÍCULOS ─
        var mainCatByArticles = mainCategories
            .OrderByDescending(c => CatArticleCount(c.Id)).ThenBy(c => c.Order)
            .FirstOrDefault();
        var mainSubs1 = mainCatByArticles != null
            ? allCategories.Where(c => c.ParentId == mainCatByArticles.Id && c.IsActive).OrderBy(c => c.Order).ToList()
            : new List<CategoryResponseDto>();
        var mainBentoCard = BuildCard(mainCatByArticles, true, mainSubs1, "Área temática principal del sitio.");

        // ── SLOT SubCardRightBottom (bottom-right grande): cat principal MÁS VISITADA ─
        var mainCatByViews = mainCategories
            .Where(c => !usedCategoryIds.Contains(c.Id))
            .OrderByDescending(c => CatViewCount(c.Id)).ThenBy(c => c.Order)
            .FirstOrDefault();
        var mainSubs2 = mainCatByViews != null
            ? allCategories.Where(c => c.ParentId == mainCatByViews.Id && c.IsActive).Take(4).ToList()
            : new List<CategoryResponseDto>();
        var subCardRightBottom = BuildCard(
            mainCatByViews,
            string.IsNullOrEmpty(mainCatByViews?.ParentId),
            mainSubs2,
            "Categoría destacada del sitio.");

        // ── SLOT SubCardRightTop (top-right compacto): subcategoría MÁS VISITADA ─────
        var subCatByViews = allSubs
            .Where(c => !usedCategoryIds.Contains(c.Id))
            .OrderByDescending(c => CatViewCount(c.Id)).ThenBy(c => c.Order)
            .FirstOrDefault();
        var subCardRightTop = BuildCard(
            subCatByViews,
            false,
            Array.Empty<CategoryResponseDto>(),
            "Especialidad destacada.");

        // ── SLOT SubCardLeft (bottom-left compacto): subcategoría con MÁS ARTÍCULOS ──
        var subCatByArticles = allSubs
            .Where(c => !usedCategoryIds.Contains(c.Id))
            .OrderByDescending(c => CatArticleCount(c.Id)).ThenBy(c => c.Order)
            .FirstOrDefault();
        var subCardLeft = BuildCard(
            subCatByArticles,
            false,
            Array.Empty<CategoryResponseDto>(),
            "Especialidad con amplia cobertura.");

        var activeCategories = allCategories.Where(c => c.IsActive).ToList();
        var subCategoriesCount = activeCategories.Count(c => !string.IsNullOrEmpty(c.ParentId));
        var publishedArticlesCount = Math.Max(totalArticleCount ?? 0L, allArticles.Count);

        var categorySection = new CategorySectionViewModel(
            Title: "Categorías principales",
            Subtitle: "Revisa las áreas temáticas más importantes de nuestro sitio web y sus especialidades.",
            MainCard: mainBentoCard,
            SubCardLeft: subCardLeft,
            SubCardRightTop: subCardRightTop,
            SubCardRightBottom: subCardRightBottom,
            AllCategories: activeCategories);

        var textoPersuasivo = new textPersuasivSectionViewModel(
            Kicker: "Catálogo Editorial Verificado",
            Title: "Encuentra el tema de tu interés",
            HighlightText: "y domina cada materia a fondo",
            Subtitle: "Reunimos investigaciones, guías prácticas y análisis rigurosos organizados por especialidades para que encuentres respuestas claras, accionables y respaldadas por evidencia en minutos.",
            TotalArticles: publishedArticlesCount,
            TotalCategories: mainCategories.Count,
            TotalSubcategories: subCategoriesCount > 0 ? subCategoriesCount : activeCategories.Count,
            ExploreHref: searchHref ?? "/publicaciones"
        );

        return new HomePageViewModel(
            Hero: hero,
            EntriesHeader: entriesHeader,
            LatestEntries: latestEntries,
            MostReadEntries: mostReadEntries,
            CategorySection: categorySection,
            TextoPersuasivo: textoPersuasivo);

    }

    private static string? GetRandomImageForCategory(
        CategoryResponseDto? category,
        IReadOnlyList<ArticleResponseDto> articles,
        IReadOnlyDictionary<string, ImageMetadataResponseDto> images,
        IReadOnlyList<CategoryResponseDto>? allCategories = null)
    {
        if (category is null) return FallbackImageForSlug(null);

        // Subcategorías de esta categoría si es padre
        var childCatIds = allCategories != null
            ? allCategories.Where(c => c.ParentId == category.Id).Select(c => c.Id).ToHashSet()
            : new HashSet<string>();

        // Artículos que pertenecen a la categoría o a sus subcategorías
        var candidateArticles = articles.Where(a =>
            (!string.IsNullOrEmpty(category.Id) && (a.CategoryId == category.Id || a.SubcategoryId == category.Id)) ||
            (!string.IsNullOrEmpty(a.CategoryId) && childCatIds.Contains(a.CategoryId)) ||
            (!string.IsNullOrEmpty(a.SubcategoryId) && childCatIds.Contains(a.SubcategoryId))
        ).ToList();

        if (candidateArticles.Count == 0 && !string.IsNullOrEmpty(category.Slug))
        {
            var slugKey = category.Slug.Replace("-", " ");
            candidateArticles = articles.Where(a =>
                (a.Slug != null && a.Slug.Contains(category.Slug, StringComparison.OrdinalIgnoreCase)) ||
                (a.Title != null && a.Title.Contains(slugKey, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        var candidateUrls = candidateArticles
            .Select(a => ResolveArticleImageUrl(a, images))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct()
            .ToList();

        if (candidateUrls.Count > 0)
        {
            var idx = Random.Shared.Next(candidateUrls.Count);
            return candidateUrls[idx];
        }

        return FallbackImageForSlug(category.Slug);
    }

    private static string? ResolveArticleImageUrl(
        ArticleResponseDto article,
        IReadOnlyDictionary<string, ImageMetadataResponseDto> images)
    {
        if (!string.IsNullOrWhiteSpace(article.CoverImageUrl))
            return article.CoverImageUrl;

        if (article.CoverImageId != null && images.TryGetValue(article.CoverImageId, out var cover))
        {
            return ImageMapper.GetUrlForWidth(cover, 1200) ?? ImageMapper.GetBestUrl(cover);
        }

        return null;
    }

    private static string FallbackImageForSlug(string? slug)
    {
        var s = slug?.ToLowerInvariant() ?? string.Empty;
        if (s.Contains("neuro"))
            return "https://images.unsplash.com/photo-1507413245164-6160d8298b31?auto=format&fit=crop&w=1200&q=80";
        if (s.Contains("nutri") || s.Contains("longevidad"))
            return "https://images.unsplash.com/photo-1490645935967-10de6ba17061?auto=format&fit=crop&w=1200&q=80";
        if (s.Contains("salud") || s.Contains("bienestar") || s.Contains("medicina"))
            return "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?auto=format&fit=crop&w=1200&q=80";
        if (s.Contains("astro") || s.Contains("espacio") || s.Contains("universo"))
            return "https://images.unsplash.com/photo-1451187580459-43490279c0fa?auto=format&fit=crop&w=1200&q=80";
        if (s.Contains("bio"))
            return "https://images.unsplash.com/photo-1532187863486-abf9dbad1b69?auto=format&fit=crop&w=1200&q=80";
        if (s.Contains("ciencia"))
            return "https://images.unsplash.com/photo-1507668077129-56e32842fceb?auto=format&fit=crop&w=1200&q=80";
        if (s.Contains("habitat") || s.Contains("sostenib") || s.Contains("energia") || s.Contains("arqui"))
            return "https://images.unsplash.com/photo-1518005020951-eccb494ad742?auto=format&fit=crop&w=1200&q=80";
        if (s.Contains("cultura") || s.Contains("cine") || s.Contains("musica") || s.Contains("arte"))
            return "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=1200&q=80";
        if (s.Contains("gastro") || s.Contains("cafe") || s.Contains("cocina"))
            return "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?auto=format&fit=crop&w=1200&q=80";

        return "https://images.unsplash.com/photo-1451187580459-43490279c0fa?auto=format&fit=crop&w=1200&q=80";
    }

    private static string CategoryName(
        ArticleResponseDto article,
        IReadOnlyDictionary<string, string> categories,
        string fallback) =>
        article.CategoryId != null && categories.TryGetValue(article.CategoryId, out var name)
            ? name
            : fallback;

    /// <summary>Misma prioridad que la vista anterior: CoverImageUrl → variante optimizada.</summary>
    private static CardImage ResolveImage(
        ArticleResponseDto article,
        IReadOnlyDictionary<string, ImageMetadataResponseDto> images)
    {
        ImageMetadataResponseDto? cover = null;
        if (article.CoverImageId != null)
            images.TryGetValue(article.CoverImageId, out cover);

        var url = !string.IsNullOrWhiteSpace(article.CoverImageUrl)
            ? article.CoverImageUrl
            : ImageMapper.GetUrlForWidth(cover, CardImageWidth) ?? ImageMapper.GetBestUrl(cover);

        var alt = !string.IsNullOrWhiteSpace(cover?.Alt) ? cover!.Alt : article.Title;

        return new CardImage(url, alt, CardImageWidth, CardImageHeight);
    }

    private static int EstimateReadingMinutes(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 3;
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(2, (int)Math.Ceiling(words / 200.0));
    }
}
