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

/// <summary>Modelo completo de la página principal.</summary>
public sealed record HomePageViewModel(
    HeroViewModel Hero,
    SectionHeaderViewModel PostsHeader,
    IReadOnlyList<PostCardViewModel> Posts,
    SectionHeaderViewModel FeaturedHeader,
    IReadOnlyList<FeaturedBlockViewModel> Featured);

/// <summary>
/// Construye el <see cref="HomePageViewModel"/> a partir de los datos entregados por
/// los servicios de la aplicación en <c>HomeController.Index</c>.
/// </summary>
public static class HomePageViewModelFactory
{
    public const int HeroCardCount = 3;
    public const int PostCount = 4;
    public const int FeaturedCount = 3;

    private const int CardImageWidth = 800;
    private const int CardImageHeight = 500; // relación 16:10

    public static HomePageViewModel Create(
        IReadOnlyList<ArticleResponseDto> recent,
        IReadOnlyList<ArticleResponseDto> featured,
        IReadOnlyDictionary<string, string> categories,
        IReadOnlyDictionary<string, ImageMetadataResponseDto> images,
        Func<string, string> articleHref,
        string? searchHref = null)
    {
        // 1. Hero: artículos marcados como destacados (fallback: los más recientes).
        var heroSource = (featured.Count > 0 ? featured : recent).Take(HeroCardCount).ToList();
        var used = heroSource.Select(a => a.Id).ToHashSet();

        // 2. Últimas publicaciones: recientes que no aparecen en el hero.
        var postSource = recent.Where(a => !used.Contains(a.Id)).Take(PostCount).ToList();
        used.UnionWith(postSource.Select(a => a.Id));

        // 3. Destacados: siguientes recientes aún no mostrados.
        var featuredSource = recent.Where(a => !used.Contains(a.Id)).Take(FeaturedCount).ToList();

        var hero = new HeroViewModel(
            Title: "Periodismo que explica el mundo",
            Subtitle: "Noticias verificadas, análisis en profundidad y reportajes sobre tecnología, economía y cultura.",
            Cards: heroSource.Select(a => new FeatureCardViewModel(
                a.Title,
                a.Summary,
                CategoryName(a, categories, "Especial"),
                articleHref(a.Slug),
                ResolveImage(a, images))).ToList());

        var postsHeader = new SectionHeaderViewModel(
            HeadingId: "ultimas-publicaciones-title",
            Title: "Últimas publicaciones",
            Subtitle: "Crónicas, reportajes e investigaciones recientes",
            LinkText: "Ver todas las publicaciones",
            LinkHref: searchHref);

        var posts = postSource.Select(a => new PostCardViewModel(
            a.Title,
            a.Summary,
            CategoryName(a, categories, "General"),
            a.PublishedAt,
            articleHref(a.Slug),
            ResolveImage(a, images))).ToList();

        var featuredHeader = new SectionHeaderViewModel(
            HeadingId: "destacados-title",
            Title: "Destacados",
            Subtitle: "Lecturas seleccionadas por la redacción");

        var featuredBlocks = featuredSource.Select(a => new FeaturedBlockViewModel(
            a.Title,
            a.Summary,
            CategoryName(a, categories, "Actualidad"),
            a.Author,
            a.ReadingTimeMinutes,
            articleHref(a.Slug),
            ResolveImage(a, images))).ToList();

        return new HomePageViewModel(hero, postsHeader, posts, featuredHeader, featuredBlocks);
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
