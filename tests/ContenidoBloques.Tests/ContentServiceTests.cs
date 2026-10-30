using Aplication.Configuration;
using Aplication.DTOs;
using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace ContenidoBloques.Tests;

public class ContentServiceTests
{
    private static ContentBodySanitizer Sanitizer() =>
        new(
            new GanssHtmlSanitizer(),
            Options.Create(new ContentSecurityOptions
            {
                MaxBlocks = 10,
                MaxTextLength = 20_000,
                MaxRichTextHtmlLength = 40_000,
                AllowedImageHosts = ["images.example.com"]
            }));

    [Fact]
    public async Task ArticleService_CreateSanitizesBodyAndCalculatesDerivedFields()
    {
        var repo = new FakeArticleRepository();
        var service = new ArticleService(repo, new FakeCategoryRepository(), Sanitizer());

        var result = await service.CreateAsync(new ArticleDto
        {
            Title = "Título de prueba",
            Slug = "titulo-de-prueba",
            Summary = "Resumen de prueba con longitud suficiente.",
            Author = "Autora",
            Status = "Publicado",
            Blocks =
            [
                new ContentParagraphBlockDto
                {
                    Text = "<strong>Texto</strong><script>alert(1)</script>"
                }
            ]
        });

        Assert.NotNull(repo.Inserted);
        Assert.Equal("Texto", ((Domine.ContentBlocks.ContentParagraphBlock)repo.Inserted!.Blocks[0]).Text.Text);
        Assert.Equal(1, repo.Inserted.ReadingTimeMinutes);
        Assert.False(string.IsNullOrWhiteSpace(repo.Inserted.SearchText));
        Assert.IsType<ContentParagraphBlockDto>(Assert.Single(result.Blocks));
    }

    [Fact]
    public async Task PageService_CreateSanitizesBlocksAndRequiresBody()
    {
        var repo = new FakePageRepository();
        var service = new PageService(repo, Sanitizer());

        await Assert.ThrowsAsync<Domine.Exceptions.DomainValidationException>(() =>
            service.CreateAsync(new PageDto
            {
                Title = "Nosotros",
                Slug = "nosotros"
            }));

        var result = await service.CreateAsync(new PageDto
        {
            Title = "Nosotros",
            Slug = "nosotros",
            Order = 1,
            Blocks = [new ContentParagraphBlockDto { Text = "Contenido" }]
        });

        Assert.NotNull(repo.Inserted);
        Assert.Single(result.Blocks);
        Assert.Equal(1, result.Order);
    }

    private sealed class FakeArticleRepository : IArticleRepository
    {
        public Article? Inserted { get; private set; }

        public Task<bool> SlugExistsAsync(string slug, string? excludeId = null) => Task.FromResult(false);
        public Task InsertAsync(Article article) { Inserted = article; return Task.CompletedTask; }

        public Task<List<Article>> GetPublishedAsync(int skip = 0, int take = 9) => throw new NotImplementedException();
        public Task<List<Article>> GetAllPublishedAsync() => throw new NotImplementedException();
        public Task<List<Article>> GetFeaturedAsync(int take = 3) => throw new NotImplementedException();
        public Task<List<Article>> GetByCategoryAsync(string categoryId, int skip = 0, int take = 9) => throw new NotImplementedException();
        public Task<long> CountByCategoryAsync(string categoryId) => throw new NotImplementedException();
        public Task<long> CountPublishedAsync() => throw new NotImplementedException();
        public Task<Article?> GetBySlugAsync(string slug) => throw new NotImplementedException();
        public Task<Article?> GetByIdAsync(string id) => throw new NotImplementedException();
        public Task<List<Article>> GetAllAsync(int skip = 0, int take = 50) => throw new NotImplementedException();
        public Task<List<Article>> SearchAsync(string text, int skip = 0, int take = 9) => throw new NotImplementedException();
        public Task<long> CountSearchAsync(string text) => throw new NotImplementedException();
        public Task UpdateAsync(Article article) => throw new NotImplementedException();
        public Task DeleteAsync(string id) => throw new NotImplementedException();
        public Task IncrementViewCountAsync(string id) => throw new NotImplementedException();
        public Task<List<Article>> GetRecommendedAsync(string currentArticleId, string? categoryId, int take = 3) => throw new NotImplementedException();
        public Task<List<Article>> GetMostReadAsync(int take = 5) => throw new NotImplementedException();
        public Task<HashSet<string>> GetCategoriesWithPublishedArticlesAsync() => throw new NotImplementedException();
        public Task<(Article? Previous, Article? Next)> GetAdjacentArticlesAsync(string currentArticleId, DateTime? publishedAt) => throw new NotImplementedException();
        public Task<List<Article>> GetScheduledToPublishAsync(DateTime now) => throw new NotImplementedException();
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public Task<List<Category>> GetAllAsync() => throw new NotImplementedException();
        public Task<List<Category>> GetMenuCategoriesAsync() => throw new NotImplementedException();
        public Task<Category?> GetByIdAsync(string id) => Task.FromResult<Category?>(null);
        public Task<Category?> GetBySlugAsync(string slug) => throw new NotImplementedException();
        public Task<List<Category>> GetByParentIdAsync(string? parentId) => throw new NotImplementedException();
        public Task InsertAsync(Category category) => throw new NotImplementedException();
        public Task UpdateAsync(Category category) => throw new NotImplementedException();
        public Task DeleteAsync(string id) => throw new NotImplementedException();
        public Task<bool> SlugExistsAsync(string slug, string? excludeId = null) => throw new NotImplementedException();
    }

    private sealed class FakePageRepository : IInformativePageRepository
    {
        public InformativePage? Inserted { get; private set; }

        public Task<bool> SlugExistsAsync(string slug, string? excludeId = null) => Task.FromResult(false);
        public Task InsertAsync(InformativePage page) { Inserted = page; return Task.CompletedTask; }

        public Task<List<InformativePage>> GetAllAsync() => throw new NotImplementedException();
        public Task<List<InformativePage>> GetFooterPagesAsync() => throw new NotImplementedException();
        public Task<InformativePage?> GetBySlugAsync(string slug) => throw new NotImplementedException();
        public Task<InformativePage?> GetByIdAsync(string id) => throw new NotImplementedException();
        public Task UpdateAsync(InformativePage page) => throw new NotImplementedException();
        public Task DeleteAsync(string id) => throw new NotImplementedException();
    }
}
