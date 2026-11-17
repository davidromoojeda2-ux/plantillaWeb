using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Domine.Entities;
using Infrastructure.Persistence;

namespace ContenidoBloques.Tests;

public class InstitutionalPagesSeedTests
{
    private static readonly string[] ExpectedSlugs =
    [
        "aviso-legal",
        "politica-de-privacidad",
        "politica-de-cookies",
        "terminos-de-uso"
    ];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [Fact]
    public void CreateTemplate_GeneraCuatroPaginasValidasConMarcadores()
    {
        var pages = InstitutionalPagesSeed.CreateTemplate();

        Assert.Equal(ExpectedSlugs, pages.Select(p => p.Slug));
        Assert.All(pages, page =>
        {
            page.ValidarInvariantes();
            Assert.True(page.ShowInFooter);
            Assert.False(string.IsNullOrWhiteSpace(page.Subtitle));
        });

        var content = Flatten(pages);
        Assert.Contains(InstitutionalPagesSeed.Dominio, content);
        Assert.Contains(InstitutionalPagesSeed.RazonSocial, content);
        Assert.Contains(InstitutionalPagesSeed.FechaActualizacion, content);
    }

    [Fact]
    public void CreateTemplate_ResuelveTodosLosMarcadoresCuandoSeAportanValores()
    {
        var values = InstitutionalPagesSeed.EditableFields.Keys
            .ToDictionary(token => token, token => $"valor-{token.Trim('{', '}')}");

        var pages = InstitutionalPagesSeed.CreateTemplate(values);
        var content = Flatten(pages);

        Assert.DoesNotContain("{{", content);
        Assert.Contains("valor-DOMINIO", content);
        Assert.Contains("valor-RAZON_SOCIAL", content);
    }

    [Fact]
    public void EditableFields_DescribeTodosLosMarcadoresDeLaPlantilla()
    {
        var pages = InstitutionalPagesSeed.CreateTemplate();
        var content = Flatten(pages);

        var tokens = Regex.Matches(content, @"\{\{[A-Z_]+\}\}")
            .Select(match => match.Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(tokens);
        Assert.All(tokens, token => Assert.True(
            InstitutionalPagesSeed.EditableFields.ContainsKey(token),
            $"El marcador {token} no está documentado en EditableFields."));
    }

    [Fact]
    public void PoliticaDeCookies_ReflejaSoloCookiesTecnicas()
    {
        var cookies = InstitutionalPagesSeed.CreateTemplate()
            .Single(page => page.Slug == "politica-de-cookies");
        var content = Flatten([cookies]);

        Assert.Contains("cookies técnicas necesarias", content);
        Assert.Contains("No se utilizan cookies analíticas", content);
        Assert.DoesNotContain("Sustituir por el inventario real", content);
        Assert.DoesNotContain("El banner debe permitir", content);
    }

    private static string Flatten(IEnumerable<InformativePage> pages)
    {
        var builder = new StringBuilder();

        foreach (var page in pages)
        {
            builder.AppendLine(page.Title);
            builder.AppendLine(page.Subtitle);

            foreach (var block in page.Blocks)
                builder.AppendLine(JsonSerializer.Serialize<object>(block, SerializerOptions));
        }

        return builder.ToString();
    }
}
