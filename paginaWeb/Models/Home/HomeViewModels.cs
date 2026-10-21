using Aplication.DTOs;
using Aplication.Mappers;
using paginaWeb.Models.Category;

namespace paginaWeb.Models.Home;

// ─── View models de presentación de la Home ──────────────────────────────────
// Son exclusivos de la capa de vista: no alteran DTOs, servicios ni la API.

/// <summary>ViewModel fuertemente tipado para los resultados y estado de la búsqueda.</summary>
public sealed record SearchViewModel(
    string Query,
    int CurrentPage,
    int TotalPages,
    long TotalCount,
    IReadOnlyList<ArticleCardViewModel> Items);

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
    CardImage Image);

/// <summary>Tarjeta de la cuadrícula "Últimas publicaciones".</summary>
public sealed record PostCardViewModel(
    string Title,
    string Excerpt,
    string Category,
    DateTime? PublishedAt,
    string Href,
    CardImage Image);

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
    int ArticleCount = 0);

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
        var heroSource = (featured.Count > 0 ? featured : recent).Take(3).ToList();
        var hero = new HeroViewModel(
            Title: "Periodismo que explica el mundo",
            Subtitle: "Noticias verificadas, análisis en profundidad y coberturas especiales de actualidad.",
            Cards: heroSource.Select(a => new FeatureCardViewModel(
                a.Title,
                a.Summary,
                CategoryName(a, catDict, "Especial"),
                articleHref(a.Slug),
                ResolveImage(a, images))).ToList());

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
        var subCardLeft = sub1 != null
            ? new CategoryBentoCardViewModel(
                Name: sub1.Name,
                Slug: sub1.Slug,
                Description: sub1.Description ?? "Explora avances, investigaciones y perspectivas científicas especializadas.",
                IsMainCategory: false,
                ParentName: mainCat.Name,
                ParentSlug: mainCat.Slug,
                Subcategories: [])
            : new CategoryBentoCardViewModel(
                Name: "Neurociencia",
                Slug: "neurociencia",
                Description: "Plasticidad cerebral, cognición y salud mental.",
                IsMainCategory: false,
                ParentName: mainCat.Name,
                ParentSlug: mainCat.Slug,
                Subcategories: []);

        // SubCard 2 (Derecha arriba - ej: Nutrición y Longevidad)
        var sub2 = mainSubs.ElementAtOrDefault(1);
        var subCardRightTop = sub2 != null
            ? new CategoryBentoCardViewModel(
                Name: sub2.Name,
                Slug: sub2.Slug,
                Description: sub2.Description ?? "Artículos de divulgación y hábitos orientados a la longevidad saludable.",
                IsMainCategory: false,
                ParentName: mainCat.Name,
                ParentSlug: mainCat.Slug,
                Subcategories: [])
            : new CategoryBentoCardViewModel(
                Name: "Nutrición y Longevidad",
                Slug: "nutricion-longevidad",
                Description: "Metabolismo, microbioma y hábitos para una vida longeva.",
                IsMainCategory: false,
                ParentName: mainCat.Name,
                ParentSlug: mainCat.Slug,
                Subcategories: []);

        // SubCard 3 (Derecha abajo grande - segunda categoría principal o subcategoría hermana)
        var secondMainCat = mainCategories.FirstOrDefault(c => c.Id != mainCat.Id)
                            ?? mainSubs.ElementAtOrDefault(2);
        var secondSubs = secondMainCat != null
            ? allCategories.Where(c => c.ParentId == secondMainCat.Id && c.IsActive).Take(4).ToList()
            : new List<CategoryResponseDto>();

        var subCardRightBottom = secondMainCat != null
            ? new CategoryBentoCardViewModel(
                Name: secondMainCat.Name,
                Slug: secondMainCat.Slug,
                Description: secondMainCat.Description ?? "Descubrimientos del universo, física, biotecnología y ciencias biológicas.",
                IsMainCategory: string.IsNullOrEmpty(secondMainCat.ParentId),
                ParentName: null,
                ParentSlug: null,
                Subcategories: secondSubs)
            : new CategoryBentoCardViewModel(
                Name: "Ciencia y Exploración",
                Slug: "ciencia-exploracion",
                Description: "Descubrimientos del universo, física, biotecnología y avances de frontera.",
                IsMainCategory: true,
                ParentName: null,
                ParentSlug: null,
                Subcategories: []);

        var mainBentoCard = new CategoryBentoCardViewModel(
            Name: mainCat.Name,
            Slug: mainCat.Slug,
            Description: mainCat.Description ?? "Neurociencia, medicina preventiva, nutrición y estilo de vida basado en evidencia.",
            IsMainCategory: true,
            ParentName: null,
            ParentSlug: null,
            Subcategories: mainSubs);

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
}
