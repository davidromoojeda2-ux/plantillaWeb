using Aplication.Content;
using Aplication.DTOs;
using Domine.Entities;
using Moq;

namespace ArticleAutomation.Tests;

public class EditorialAuthorsTests
{
    [Theory]
    [InlineData("guias-por-comunidad-autonoma")]
    [InlineData("ayudas-y-subvenciones")]
    [InlineData("comunidades-de-vecinos")]
    [InlineData("ahorro-y-rentabilidad")]
    public void Resolve_Devuelve_Kely_Para_Sus_Secciones(string slug)
    {
        var author = EditorialAuthors.Resolve(slug);

        Assert.Equal("Kely Pantoja", author.Name);
        Assert.Equal("Redactora", author.Role);
    }

    [Theory]
    [InlineData("placas-solares")]
    [InlineData("baterias-solares")]
    [InlineData("instalacion-y-mantenimiento")]
    public void Resolve_Devuelve_El_Grupo_Para_El_Resto_De_Secciones(string slug)
    {
        var author = EditorialAuthors.Resolve(slug);

        Assert.Equal("Grupo de Redactores de unautonomo", author.Name);
        Assert.Equal("Equipo editorial", author.Role);
    }

    [Fact]
    public void Las_Dos_Firmas_Tienen_Descripciones_Diferentes()
    {
        Assert.NotEqual(EditorialAuthors.KelyPantoja.Bio, EditorialAuthors.EditorialGroup.Bio);
        Assert.False(string.IsNullOrWhiteSpace(EditorialAuthors.KelyPantoja.Bio));
        Assert.False(string.IsNullOrWhiteSpace(EditorialAuthors.EditorialGroup.Bio));
    }

    [Fact]
    public async Task La_Automatizacion_Firma_Con_Kely_Si_La_Categoria_Es_Suya()
    {
        ArticleDto? created = null;
        var harness = new AutomationHarness()
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["tecnologia"],
                AutoStandards = false,
                CategoryIds = ["cat-kely"]
            });

        harness.Categories.Setup(c => c.GetAllAsync()).ReturnsAsync(
        [
            new CategoryResponseDto
            {
                Id = "cat-kely",
                Name = "Ayudas y subvenciones",
                Slug = "ayudas-y-subvenciones"
            }
        ]);
        harness.Articles
            .Setup(a => a.CreateAsync(It.IsAny<ArticleDto>()))
            .Callback<ArticleDto>(dto => created = dto)
            .ReturnsAsync((ArticleDto dto) => new ArticleResponseDto
            {
                Id = "id1",
                Title = dto.Title,
                Status = "Borrador"
            });

        await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Kely Pantoja", created!.Author);
        Assert.Equal("Redactora", created.AuthorRole);
        Assert.Equal(EditorialAuthors.KelyPantoja.Bio, created.AuthorBio);
    }

    [Fact]
    public async Task La_Automatizacion_Firma_Con_El_Grupo_Si_La_Categoria_No_Es_De_Kely()
    {
        ArticleDto? created = null;
        var harness = new AutomationHarness()
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["tecnologia"],
                AutoStandards = false,
                CategoryIds = ["cat-grupo"]
            });

        harness.Categories.Setup(c => c.GetAllAsync()).ReturnsAsync(
        [
            new CategoryResponseDto
            {
                Id = "cat-grupo",
                Name = "Placas solares",
                Slug = "placas-solares"
            }
        ]);
        harness.Articles
            .Setup(a => a.CreateAsync(It.IsAny<ArticleDto>()))
            .Callback<ArticleDto>(dto => created = dto)
            .ReturnsAsync((ArticleDto dto) => new ArticleResponseDto
            {
                Id = "id1",
                Title = dto.Title,
                Status = "Borrador"
            });

        await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Grupo de Redactores de unautonomo", created!.Author);
        Assert.Equal("Equipo editorial", created.AuthorRole);
        Assert.Equal(EditorialAuthors.EditorialGroup.Bio, created.AuthorBio);
    }
}
