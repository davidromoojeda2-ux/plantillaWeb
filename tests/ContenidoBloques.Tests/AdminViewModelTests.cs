using Aplication.DTOs;
using Aplication.DTOs.ContentBlocks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using paginaWeb.Areas.Admin.Models;
using paginaWeb.Helpers;

namespace ContenidoBloques.Tests;

public class AdminViewModelTests
{
    [Fact]
    public void FormHelper_ParsesValidJsonAndReportsInvalidJson()
    {
        var validState = new ModelStateDictionary();
        Assert.True(ContentBlockFormHelper.TryParse(
            """[{ "type": "paragraph", "text": "Hola" }]""",
            validState,
            out var blocks));

        Assert.True(validState.IsValid);
        Assert.IsType<ContentParagraphBlockDto>(Assert.Single(blocks));

        var invalidJson = """[{ "type": "video", "url": "https://example.com" }]""";
        var invalidState = new ModelStateDictionary();
        Assert.False(ContentBlockFormHelper.TryParse(invalidJson, invalidState, out var invalidBlocks));

        Assert.False(invalidState.IsValid);
        Assert.Empty(invalidBlocks);
        Assert.Contains("BlocksJson", invalidState.Keys);
    }

    [Fact]
    public void PageFormViewModel_RoundTripsBlocksThroughJson()
    {
        var page = new PageResponseDto
        {
            Id = "page-1",
            Title = "Página de prueba",
            Slug = "pagina-de-prueba",
            Subtitle = "Subtítulo",
            Order = 3,
            ShowInFooter = true,
            Blocks =
            [
                new ContentHeadingBlockDto { Level = 2, Text = "Sección" },
                new ContentTableBlockDto
                {
                    Headers = ["A", "B"],
                    Rows = [["1", "2"]]
                }
            ]
        };

        var model = PageFormViewModel.FromDto(page);
        var parsed = Aplication.Serialization.ContentBlockJson.Deserialize(model.BlocksJson);
        var roundTrip = model.ToDto(parsed);

        Assert.Equal(page.Id, model.Id);
        Assert.Equal(page.Title, model.Title);
        Assert.Equal(page.Slug, model.Slug);
        Assert.Equal(page.Subtitle, model.Subtitle);
        Assert.Equal(page.Order, model.Order);
        Assert.Equal(page.ShowInFooter, model.ShowInFooter);
        Assert.Equal(2, roundTrip.Blocks.Count);
        Assert.IsType<ContentHeadingBlockDto>(roundTrip.Blocks[0]);
        var table = Assert.IsType<ContentTableBlockDto>(roundTrip.Blocks[1]);
        Assert.Equal(["A", "B"], table.Headers);
        Assert.Equal(["1", "2"], table.Rows[0]);
    }

    [Fact]
    public void ArticleFormViewModel_KeepsBlocksAndEditorMetadata()
    {
        var article = new ArticleResponseDto
        {
            Id = "article-1",
            Title = "Artículo de prueba",
            Slug = "articulo-de-prueba",
            Summary = "Resumen de prueba con longitud suficiente.",
            Author = "Autora",
            AuthorRole = "Editora",
            CategoryId = "category-1",
            Tags = ["uno", "dos"],
            CoverImageId = "cover-1",
            ImageIds = ["cover-1", "body-1"],
            Featured = true,
            Status = "Publicado",
            Blocks =
            [
                new ContentParagraphBlockDto { Text = "<strong>Hola</strong>" },
                new ContentImageBlockDto { ImageId = "body-1", Alt = "Foto" }
            ]
        };

        var model = ArticleFormViewModel.FromDto(article, "https://cdn.example.com/cover.webp");
        var parsed = Aplication.Serialization.ContentBlockJson.Deserialize(model.BlocksJson);
        var dto = model.ToDto(parsed);

        Assert.Equal(article.Id, model.Id);
        Assert.Equal(article.Title, model.Title);
        Assert.Equal(article.Slug, model.Slug);
        Assert.Equal(article.Summary, model.Summary);
        Assert.Equal("https://cdn.example.com/cover.webp", model.CoverImageUrl);
        Assert.Equal(article.ImageIds, model.ImageIds);
        Assert.Equal(2, dto.Blocks.Count);
        Assert.Equal("<strong>Hola</strong>", Assert.IsType<ContentParagraphBlockDto>(dto.Blocks[0]).Text);
        Assert.Equal("body-1", Assert.IsType<ContentImageBlockDto>(dto.Blocks[1]).ImageId);
    }
}
