using Aplication.DTOs.ContentBlocks;
using Aplication.Services;

namespace ContenidoBloques.Tests;

public class ArticleFormValidatorTests
{
    [Fact]
    public void Un_articulo_completo_no_tiene_errores()
    {
        var result = ArticleFormValidator.Validate(
            "Título de prueba",
            "Resumen suficientemente largo para validar el artículo.",
            "titulo-de-prueba",
            "Redacción",
            "cat-1",
            "/uploads/portada.webp",
            "Publicado",
            ValidBlocks());

        Assert.Empty(result.Errors);
        Assert.Contains("https://example.com/fuente", result.Links);
    }

    [Fact]
    public void Detecta_campos_obligatorios_vacios()
    {
        var result = ArticleFormValidator.Validate(
            "", "", "", "", null, null, "Publicado", ValidBlocks());

        Assert.Contains(result.Errors, e => e.Contains("título", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("resumen", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("slug", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("autor", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, w => w.Contains("categoría", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, w => w.Contains("portada", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Rechaza_slug_con_mayusculas_espacios_o_acentos()
    {
        var result = ArticleFormValidator.Validate(
            "Título de prueba",
            "Resumen suficientemente largo para validar el artículo.",
            "Título con espacios",
            "Redacción",
            "cat-1",
            "/uploads/portada.webp",
            "Borrador",
            ValidBlocks());

        Assert.Contains(result.Errors, e => e.Contains("slug", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Detecta_encabezado_inicial_y_h1()
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentHeadingBlockDto { Level = 2, Text = "Empieza con encabezado" },
            new ContentHeadingBlockDto { Level = 1, Text = "Título principal" },
            new ContentParagraphBlockDto { Text = "Contenido" }
        };

        var result = ArticleFormValidator.Validate(
            "Título de prueba",
            "Resumen suficientemente largo para validar el artículo.",
            "titulo-de-prueba",
            "Redacción",
            "cat-1",
            "/uploads/portada.webp",
            "Borrador",
            blocks);

        Assert.Contains(result.Errors, e => e.Contains("empezar con un encabezado", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("nivel 1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Detecta_listas_con_menos_de_dos_elementos()
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto { Text = "Intro" },
            new ContentBulletListBlockDto { Items = ["Solo uno"] },
            new ContentParagraphBlockDto { Text = "Cierre" }
        };

        var result = ArticleFormValidator.Validate(
            "Título de prueba",
            "Resumen suficientemente largo para validar el artículo.",
            "titulo-de-prueba",
            "Redacción",
            "cat-1",
            "/uploads/portada.webp",
            "Borrador",
            blocks);

        Assert.Contains(result.Errors, e => e.Contains("bloques vacíos", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Extrae_enlaces_unicos_y_omite_esquemas_no_http()
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto
            {
                Text = "Ver <a href=\"https://example.com/a\">A</a> y <a href='https://example.com/a'>A repetido</a>."
            },
            new ContentBulletListBlockDto
            {
                Items =
                [
                    "<a href=\"mailto:hola@example.com\">Correo</a>",
                    "<a href=\"#seccion\">Ancla</a>",
                    "<a href=\"/articulos/otro\">Interno</a>"
                ]
            }
        };

        var links = ArticleFormValidator.ExtractLinks(blocks);

        Assert.Equal(2, links.Count);
        Assert.Contains("https://example.com/a", links);
        Assert.Contains("/articulos/otro", links);
        Assert.DoesNotContain("mailto:hola@example.com", links);
        Assert.DoesNotContain("#seccion", links);
    }

    private static List<ContentBlockDto> ValidBlocks() =>
    [
        new ContentParagraphBlockDto
        {
            Text = "Intro con <a href=\"https://example.com/fuente\">fuente</a>."
        },
        new ContentHeadingBlockDto { Level = 2, Text = "Sección" },
        new ContentParagraphBlockDto { Text = "Contenido desarrollado." },
        new ContentBulletListBlockDto { Items = ["Uno", "Dos"] }
    ];
}
