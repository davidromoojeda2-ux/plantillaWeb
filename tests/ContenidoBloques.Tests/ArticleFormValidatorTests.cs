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

    [Fact]
    public void Los_parrafos_largos_generan_avisos_sin_bloquear()
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto
            {
                Text = string.Join(' ', Enumerable.Repeat("palabra", 200))
            }
        };

        var profile = ParagraphStyleProfile.Resolve("news", null, "es");

        var result = ArticleFormValidator.Validate(
            "Título de prueba",
            "Resumen suficientemente largo para validar el artículo.",
            "titulo-de-prueba",
            "Redacción",
            "cat-1",
            "/uploads/portada.webp",
            "Publicado",
            blocks,
            profile);

        Assert.DoesNotContain(result.Errors, e => e.Contains("párrafo", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, w => w.Contains("palabras", StringComparison.OrdinalIgnoreCase));

        var issue = Assert.Single(
            result.EffectiveIssues, i => i.Code == "style-length-long");
        Assert.Contains(0, issue.BlockIndexes!);
    }

    [Fact]
    public void Las_observaciones_de_estilo_son_individuales_por_bloque()
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto
            {
                Text = string.Join(' ', Enumerable.Repeat("palabra", 120))
            },
            new ContentParagraphBlockDto
            {
                Text = string.Join(' ', Enumerable.Repeat("palabra", 80))
            }
        };

        var profile = ParagraphStyleProfile.Resolve("news", null, "es");

        var result = ArticleFormValidator.Validate(
            "Título de prueba",
            "Resumen suficientemente largo para validar el artículo.",
            "titulo-de-prueba",
            "Redacción",
            "cat-1",
            "/uploads/portada.webp",
            "Publicado",
            blocks,
            profile);

        var blockIssues = result.EffectiveBlockIssues
            .Where(issue => issue.Code == "style-length")
            .OrderBy(issue => issue.BlockIndexes![0])
            .ToList();

        Assert.Equal(2, blockIssues.Count);
        Assert.Equal([0], blockIssues[0].BlockIndexes);
        Assert.Equal([1], blockIssues[1].BlockIndexes);
        Assert.Contains("120 palabras", blockIssues[0].Message);
        Assert.Contains("80 palabras", blockIssues[1].Message);

        // El resumen global sigue agrupando, pero las notas de bloque no lo repiten.
        Assert.Contains(result.Warnings, w => w.Contains("2 párrafos", StringComparison.Ordinal));
        Assert.DoesNotContain(blockIssues, issue => issue.Message.Contains("2 párrafos", StringComparison.Ordinal));
    }

    [Fact]
    public void La_observacion_de_bloque_vacio_es_individual()
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto(),
            new ContentHeadingBlockDto { Level = 2, Text = "" },
            new ContentParagraphBlockDto { Text = "Contenido con extensión suficiente." }
        };

        var result = ArticleFormValidator.Validate(
            "Título de prueba",
            "Resumen suficientemente largo para validar el artículo.",
            "titulo-de-prueba",
            "Redacción",
            "cat-1",
            "/uploads/portada.webp",
            "Publicado",
            blocks,
            ParagraphStyleProfile.Resolve("news", null, "es"));

        var emptyIssues = result.EffectiveBlockIssues
            .Where(issue => issue.Code == "empty")
            .OrderBy(issue => issue.BlockIndexes![0])
            .ToList();

        Assert.Equal(2, emptyIssues.Count);
        Assert.Contains("párrafo está vacío", emptyIssues[0].Message);
        Assert.Contains("encabezado está vacío", emptyIssues[1].Message);
    }

    [Fact]
    public void Los_errores_de_campo_apuntan_al_input()
    {
        var result = ArticleFormValidator.Validate(
            "", "", "", "", null, null, "Publicado", ValidBlocks());

        var titleIssue = Assert.Single(
            result.EffectiveIssues, issue => issue.Field == "titleInput");

        Assert.Equal("error", titleIssue.Severity);
        Assert.Equal("required", titleIssue.Code);
    }

    [Fact]
    public void Localiza_el_bloque_que_contiene_un_enlace()
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto { Text = "Sin enlaces." },
            new ContentParagraphBlockDto
            {
                Text = "Ver <a href=\"https://example.com/a\">A</a>."
            }
        };

        var indexes = ArticleFormValidator.FindBlocksWithLink(
            blocks, "https://example.com/a");

        Assert.Equal([1], indexes);
    }

    [Fact]
    public void Localiza_el_enlace_aunque_cambie_la_barra_final()
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto
            {
                Text = "Ver <a href=\"http://localhost:5034/articulos/roto\">artículo</a>."
            }
        };

        var indexes = ArticleFormValidator.FindBlocksWithLink(
            blocks, "http://localhost:5034/articulos/roto/",
            new Uri("http://localhost:5034"));

        Assert.Equal([0], indexes);
    }

    [Fact]
    public void Quita_los_enlaces_caidos_y_conserva_el_texto()
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto
            {
                Text = "Mira <a href=\"http://localhost:5034/articulos/roto\">esto</a> " +
                       "y <a href=\"/articulos/ok\">aquello</a>."
            }
        };

        ArticleFormValidator.RemoveLinks(
            blocks,
            ["http://localhost:5034/articulos/roto/"],
            new Uri("http://localhost:5034"));

        var text = ((ContentParagraphBlockDto)blocks[0]).Text;

        Assert.Contains("Mira esto", text);
        Assert.DoesNotContain("articulos/roto", text);
        Assert.Contains("/articulos/ok", text);
    }

    [Fact]
    public void Reemplaza_el_href_conservando_texto_y_atributos()
    {
        var blocks = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto
            {
                Text = "Mira <a href=\"http://localhost:5034/articulos/roto\" class=\"x\">esto</a>."
            }
        };

        var count = ArticleFormValidator.ReplaceLinks(
            blocks,
            new Dictionary<string, string>
            {
                ["http://localhost:5034/articulos/roto/"] = "/articulos/valido"
            },
            new Uri("http://localhost:5034"));

        var text = ((ContentParagraphBlockDto)blocks[0]).Text;

        Assert.Equal(1, count);
        Assert.Contains("href=\"/articulos/valido\"", text);
        Assert.Contains("class=\"x\"", text);
        Assert.Contains(">esto</a>", text);
    }

    [Fact]
    public void Restaura_el_href_original_si_la_ia_lo_cambio()
    {
        var original = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto
            {
                Text = "Ver <a href=\"/articulos/original\">fuente</a>."
            }
        };
        var corrected = new List<ContentBlockDto>
        {
            new ContentParagraphBlockDto
            {
                Text = "Ver <a href=\"/articulos/inventado\">fuente</a>."
            }
        };

        var count = ArticleFormValidator.RestoreLinkHrefs(corrected, original);
        var text = ((ContentParagraphBlockDto)corrected[0]).Text;

        Assert.Equal(1, count);
        Assert.Contains("/articulos/original", text);
        Assert.DoesNotContain("/articulos/inventado", text);
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
