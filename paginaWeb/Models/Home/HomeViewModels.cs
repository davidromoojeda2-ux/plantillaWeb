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
    string? CustomClass = null)
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
            AsGridItem: asGridItem
        );
    }
}

/// <summary>Bloque de la fila "Destacados".</summary>
public sealed record FeaturedBlockViewModel(
    string Title,
    string Excerpt,
    string Category,
    string Author,
    int ReadingMinutes,
    string Href,
    CardImage Image);

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
    int ArticleCount = 0,
    string? ImageUrl = null);

/// <summary>Sección 'Encuentra el tema de tu interés' estructurada en formato Bento de 2 columnas.</summary>
public sealed record CategorySectionViewModel(
    string Title,
    string Subtitle,
    CategoryBentoCardViewModel MainCard,
    CategoryBentoCardViewModel SubCardLeft,
    CategoryBentoCardViewModel SubCardRightTop,
    CategoryBentoCardViewModel SubCardRightBottom,
    IReadOnlyList<CategoryResponseDto> AllCategories);

/// <summary>Modelo completo de la página principal.</summary>
public sealed record HomePageViewModel(
    HeroViewModel Hero,
    SectionHeaderViewModel EntriesHeader,
    IReadOnlyList<PostCardViewModel> FeaturedEntries,
    IReadOnlyList<PostCardViewModel> RecentEntries,
    CategorySectionViewModel CategorySection,
    SectionHeaderViewModel? PostsHeader = null,
    IReadOnlyList<PostCardViewModel>? Posts = null,
    SectionHeaderViewModel? FeaturedHeader = null,
    IReadOnlyList<FeaturedBlockViewModel>? Featured = null);

/// <summary>
/// Construye el <see cref="HomePageViewModel"/> a partir de los datos entregados por
/// los servicios de la aplicación en <c>HomeController.Index</c>.
/// </summary>
public static class HomePageViewModelFactory
{
    public const int ColumnEntryCount = 3;

    private const int CardImageWidth = 800;
    private const int CardImageHeight = 500; // relación 16:10

    public static HomePageViewModel Create(
        IReadOnlyList<ArticleResponseDto> recent,
        IReadOnlyList<ArticleResponseDto> featured,
        IReadOnlyList<CategoryResponseDto> allCategories,
        IReadOnlyDictionary<string, ImageMetadataResponseDto> images,
        Func<string, string> articleHref,
        string? searchHref = null)
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
                EstimateReadingMinutes(a.Content ?? a.Summary))).ToList());

        // 2. Últimas entradas en dos columnas (3 principales/destacadas a la izquierda, 3 recientes a la derecha)
        var featuredEntriesSource = (featured.Count >= 3 ? featured : recent).Take(ColumnEntryCount).ToList();
        var featuredIds = featuredEntriesSource.Select(a => a.Id).ToHashSet();

        var recentEntriesSource = recent.Where(a => !featuredIds.Contains(a.Id)).Take(ColumnEntryCount).ToList();
        if (recentEntriesSource.Count < ColumnEntryCount)
        {
            recentEntriesSource = recent.Take(ColumnEntryCount).ToList();
        }

        var featuredEntries = featuredEntriesSource.Select(a => new PostCardViewModel(
            a.Title,
            a.Summary,
            CategoryName(a, catDict, "Destacado"),
            a.PublishedAt,
            articleHref(a.Slug),
            ResolveImage(a, images))).ToList();

        var recentEntries = recentEntriesSource.Select(a => new PostCardViewModel(
            a.Title,
            a.Summary,
            CategoryName(a, catDict, "Actualidad"),
            a.PublishedAt,
            articleHref(a.Slug),
            ResolveImage(a, images))).ToList();

        var entriesHeader = new SectionHeaderViewModel(
            HeadingId: "ultimas-entradas-title",
            Title: "Últimas entradas",
            Subtitle: "Noticias principales y coberturas recientes seleccionadas por nuestra redacción",
            LinkText: "Ver todas",
            LinkHref: searchHref);

        // 3. Sección Bento: 'Encuentra el tema de tu interés'
        var allArticles = recent.Concat(featured).DistinctBy(a => a.Id).ToList();

        var mainCategories = allCategories
            .Where(c => string.IsNullOrEmpty(c.ParentId) && c.IsActive)
            .OrderBy(c => c.Order)
            .ToList();

        // Buscar 'Salud y Bienestar' como principal (o la primera con subcategorías)
        var mainCat = mainCategories.FirstOrDefault(c => c.Slug.Contains("salud", StringComparison.OrdinalIgnoreCase))
                      ?? mainCategories.FirstOrDefault(c => allCategories.Any(sub => sub.ParentId == c.Id && sub.IsActive))
                      ?? mainCategories.FirstOrDefault()
                      ?? new CategoryResponseDto { Name = "Salud y Bienestar", Slug = "salud-bienestar", Description = "Medicina preventiva, bienestar integral, neurociencia y vida longeva." };

        var mainSubs = allCategories
            .Where(c => c.ParentId == mainCat.Id && c.IsActive)
            .OrderBy(c => c.Order)
            .ToList();

        // SubCard 1 (Izquierda abajo - ej: Neurociencia)
        var sub1 = mainSubs.ElementAtOrDefault(0);
        var sub1Cat = sub1 ?? new CategoryResponseDto { Name = "Neurociencia", Slug = "neurociencia", Description = "Plasticidad cerebral, cognición y salud mental." };
        var subCardLeft = new CategoryBentoCardViewModel(
            Name: sub1Cat.Name,
            Slug: sub1Cat.Slug,
            Description: sub1Cat.Description ?? "Explora avances, investigaciones y perspectivas científicas especializadas.",
            IsMainCategory: false,
            ParentName: mainCat.Name,
            ParentSlug: mainCat.Slug,
            Subcategories: [],
            ImageUrl: GetRandomImageForCategory(sub1Cat, allArticles, images, allCategories));

        // SubCard 2 (Derecha arriba - ej: Nutrición y Longevidad)
        var sub2 = mainSubs.ElementAtOrDefault(1);
        var sub2Cat = sub2 ?? new CategoryResponseDto { Name = "Nutrición y Longevidad", Slug = "nutricion-longevidad", Description = "Metabolismo, microbioma y hábitos para una vida longeva." };
        var subCardRightTop = new CategoryBentoCardViewModel(
            Name: sub2Cat.Name,
            Slug: sub2Cat.Slug,
            Description: sub2Cat.Description ?? "Artículos de divulgación y hábitos orientados a la longevidad saludable.",
            IsMainCategory: false,
            ParentName: mainCat.Name,
            ParentSlug: mainCat.Slug,
            Subcategories: [],
            ImageUrl: GetRandomImageForCategory(sub2Cat, allArticles, images, allCategories));

        // SubCard 3 (Derecha abajo grande - segunda categoría principal o subcategoría hermana)
        var secondMainCat = mainCategories.FirstOrDefault(c => c.Id != mainCat.Id)
                            ?? mainSubs.ElementAtOrDefault(2);
        var secondSubs = secondMainCat != null
            ? allCategories.Where(c => c.ParentId == secondMainCat.Id && c.IsActive).Take(4).ToList()
            : new List<CategoryResponseDto>();
        var secondCat = secondMainCat ?? new CategoryResponseDto { Name = "Ciencia y Exploración", Slug = "ciencia-exploracion", Description = "Descubrimientos del universo, física, biotecnología y avances de frontera." };

        var subCardRightBottom = new CategoryBentoCardViewModel(
            Name: secondCat.Name,
            Slug: secondCat.Slug,
            Description: secondCat.Description ?? "Descubrimientos del universo, física, biotecnología y ciencias biológicas.",
            IsMainCategory: string.IsNullOrEmpty(secondCat.ParentId),
            ParentName: null,
            ParentSlug: null,
            Subcategories: secondSubs,
            ImageUrl: GetRandomImageForCategory(secondCat, allArticles, images, allCategories));

        var mainBentoCard = new CategoryBentoCardViewModel(
            Name: mainCat.Name,
            Slug: mainCat.Slug,
            Description: mainCat.Description ?? "Neurociencia, medicina preventiva, nutrición y estilo de vida basado en evidencia.",
            IsMainCategory: true,
            ParentName: null,
            ParentSlug: null,
            Subcategories: mainSubs,
            ImageUrl: GetRandomImageForCategory(mainCat, allArticles, images, allCategories));

        var categorySection = new CategorySectionViewModel(
            Title: "Encuentra el tema de tu interés",
            Subtitle: "Explora nuestras áreas temáticas y navega por sus especialidades y coberturas exclusivas.",
            MainCard: mainBentoCard,
            SubCardLeft: subCardLeft,
            SubCardRightTop: subCardRightTop,
            SubCardRightBottom: subCardRightBottom,
            AllCategories: allCategories.Where(c => c.IsActive).ToList());

        return new HomePageViewModel(
            Hero: hero,
            EntriesHeader: entriesHeader,
            FeaturedEntries: featuredEntries,
            RecentEntries: recentEntries,
            CategorySection: categorySection,
            PostsHeader: entriesHeader,
            Posts: recentEntries,
            FeaturedHeader: entriesHeader,
            Featured: []);
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
