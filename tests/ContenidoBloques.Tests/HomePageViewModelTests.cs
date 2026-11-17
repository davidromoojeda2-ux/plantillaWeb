using Aplication.Configuration;
using Aplication.DTOs;
using paginaWeb.Models.Home;

namespace ContenidoBloques.Tests;

public class HomePageViewModelTests
{
    [Fact]
    public void El_hero_completa_los_cupos_con_los_ultimos_articulos()
    {
        var recent = Enumerable.Range(1, 7)
            .Select(i => Article($"reciente-{i}", DateTime.Today.AddDays(-i)))
            .ToList();

        var featured = new List<ArticleResponseDto>
        {
            Article("destacado-1", DateTime.Today.AddDays(-10), featured: true)
        };

        var model = CreateModel(recent, featured);

        Assert.Equal(ArticleDisplayLimits.HeroFeaturedLimit, model.Hero.Cards.Count);
        Assert.Equal("destacado-1", model.Hero.Cards[0].Title);
        Assert.Contains(model.Hero.Cards, card => card.Title == "reciente-1");
        Assert.Contains(model.Hero.Cards, card => card.Title == "reciente-4");
    }

    [Fact]
    public void El_hero_no_supera_el_limite_configurado()
    {
        var featured = Enumerable.Range(1, 8)
            .Select(i => Article($"destacado-{i}", DateTime.Today.AddDays(-i), featured: true))
            .ToList();

        var model = CreateModel([], featured);

        Assert.Equal(ArticleDisplayLimits.HeroFeaturedLimit, model.Hero.Cards.Count);
        Assert.Equal("destacado-1", model.Hero.Cards[0].Title);
    }

    private static HomePageViewModel CreateModel(
        IReadOnlyList<ArticleResponseDto> recent,
        IReadOnlyList<ArticleResponseDto> featured) =>
        HomePageViewModelFactory.Create(
            recent,
            featured,
            [],
            new Dictionary<string, ImageMetadataResponseDto>(),
            slug => $"/articulos/{slug}");

    private static ArticleResponseDto Article(string slug, DateTime publishedAt, bool featured = false) => new()
    {
        Id = slug,
        Title = slug,
        Slug = slug,
        Summary = "Resumen de prueba",
        Author = "Redacción",
        Status = "Publicado",
        PublishedAt = publishedAt,
        Featured = featured
    };
}
