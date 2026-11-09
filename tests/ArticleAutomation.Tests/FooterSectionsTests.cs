using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Moq;

namespace ArticleAutomation.Tests;

public class FooterSectionsTests
{
    [Fact]
    public async Task GetFooterSectionsAsync_Counts_Each_Article_Once_Per_Root()
    {
        var categoryRepo = new Mock<ICategoryRepository>();
        categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Category { Id = "a", Name = "A", Slug = "a", Order = 1 },
            new Category { Id = "a1", ParentId = "a", Name = "A1", Slug = "a1" },
            new Category { Id = "b", Name = "B", Slug = "b", Order = 0 },
            new Category { Id = "c", Name = "C", Slug = "c" }
        ]);

        var articleRepo = new Mock<IArticleRepository>();
        articleRepo.Setup(r => r.GetPublishedCategorySignalsAsync()).ReturnsAsync(
        [
            // Artículo asignado a la raíz y a su subcategoría: debe contar una sola vez.
            new CategoryArticleSignalDto { CategoryId = "a", SubcategoryId = "a1", ViewCount = 10 },
            new CategoryArticleSignalDto { CategoryId = "b", ViewCount = 15 }
        ]);

        var service = new CategoryService(categoryRepo.Object, articleRepo.Object);

        var result = await service.GetFooterSectionsAsync();

        Assert.Equal(["B", "A"], result.Select(s => s.Name));
    }

    [Fact]
    public async Task GetFooterSectionsAsync_Uses_Article_Count_When_Views_Are_Equal()
    {
        var categoryRepo = new Mock<ICategoryRepository>();
        categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Category { Id = "a", Name = "Con más contenido", Slug = "a", Order = 0 },
            new Category { Id = "b", Name = "Con menos contenido", Slug = "b", Order = 1 }
        ]);

        var articleRepo = new Mock<IArticleRepository>();
        articleRepo.Setup(r => r.GetPublishedCategorySignalsAsync()).ReturnsAsync(
        [
            new CategoryArticleSignalDto { CategoryId = "a", ViewCount = 0 },
            new CategoryArticleSignalDto { CategoryId = "a", ViewCount = 0 },
            new CategoryArticleSignalDto { CategoryId = "b", ViewCount = 0 }
        ]);

        var service = new CategoryService(categoryRepo.Object, articleRepo.Object);

        var result = await service.GetFooterSectionsAsync();

        Assert.Equal(["Con más contenido", "Con menos contenido"], result.Select(s => s.Name));
    }

    [Fact]
    public async Task GetFooterSectionsAsync_Excludes_Sections_Without_Published_Content()
    {
        var categoryRepo = new Mock<ICategoryRepository>();
        categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Category { Id = "a", Name = "Con contenido", Slug = "a" },
            new Category { Id = "b", Name = "Sin contenido", Slug = "b" }
        ]);

        var articleRepo = new Mock<IArticleRepository>();
        articleRepo.Setup(r => r.GetPublishedCategorySignalsAsync()).ReturnsAsync(
        [
            new CategoryArticleSignalDto { CategoryId = "a", ViewCount = 3 }
        ]);

        var service = new CategoryService(categoryRepo.Object, articleRepo.Object);

        var result = await service.GetFooterSectionsAsync();

        Assert.Single(result);
        Assert.Equal("Con contenido", result[0].Name);
    }
}
