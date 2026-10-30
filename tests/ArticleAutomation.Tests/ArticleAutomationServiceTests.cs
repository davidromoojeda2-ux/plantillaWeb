using Aplication.DTOs;
using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ArticleAutomation.Tests;

public class ArticleAutomationServiceTests
{
    [Fact]
    public async Task RunOnceAsync_Generates_And_Creates_Article()
    {
        var discovery = new Mock<ITopicDiscoveryService>();
        discovery.Setup(d => d.DiscoverAsync(It.IsAny<TopicDiscoveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiscoveredTopic> { new() { Title = "Noticia de prueba", Source = "fuente" } });

        var ai = new Mock<IAiArticleGenerator>();
        ai.Setup(a => a.GenerateAsync(It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedArticle
            {
                Title = "Noticia de prueba",
                Summary = "Resumen de prueba",
                Blocks = new List<ContentBlockDto> { new ContentParagraphBlockDto { Text = "Contenido" } },
                Tags = ["uno"]
            });

        var articles = new Mock<IArticleService>();
        articles.Setup(a => a.CreateAsync(It.IsAny<ArticleDto>()))
            .ReturnsAsync(new ArticleResponseDto { Id = "id1", Title = "Noticia de prueba", Status = "Borrador" });

        var articleRepo = new Mock<IArticleRepository>();
        articleRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);

        var categories = new Mock<ICategoryService>();
        categories.Setup(c => c.GetAllAsync()).ReturnsAsync(new List<CategoryResponseDto>());

        var state = new Mock<IAutomationStateRepository>();
        state.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new AutomationSettings { Enabled = true, BaseTopics = ["tecnologia"] });
        state.Setup(s => s.HashExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        state.Setup(s => s.InsertRunAsync(It.IsAny<AutomationRun>())).Returns(Task.CompletedTask);
        state.Setup(s => s.UpdateRunAsync(It.IsAny<AutomationRun>())).Returns(Task.CompletedTask);

        var service = new ArticleAutomationService(
            discovery.Object, ai.Object, articles.Object, articleRepo.Object,
            categories.Object, state.Object, new AutomationCoordinator(),
            NullLogger<ArticleAutomationService>.Instance);

        var run = await service.RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Equal(AutomationRunStatus.Completado, run!.Status);
        Assert.Single(run.GeneratedArticles);
        Assert.Equal("id1", run.GeneratedArticles[0].ArticleId);
        articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Once);
    }

    [Fact]
    public async Task RunOnceAsync_Duplicate_Topic_Is_Flagged_For_Decision()
    {
        var discovery = new Mock<ITopicDiscoveryService>();
        discovery.Setup(d => d.DiscoverAsync(It.IsAny<TopicDiscoveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiscoveredTopic> { new() { Title = "Noticia repetida" } });

        var ai = new Mock<IAiArticleGenerator>();
        var articles = new Mock<IArticleService>();

        var articleRepo = new Mock<IArticleRepository>();
        articleRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(true);

        var categories = new Mock<ICategoryService>();
        categories.Setup(c => c.GetAllAsync()).ReturnsAsync(new List<CategoryResponseDto>());

        var state = new Mock<IAutomationStateRepository>();
        state.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new AutomationSettings { Enabled = true, BaseTopics = ["tecnologia"] });
        state.Setup(s => s.HashExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        state.Setup(s => s.InsertRunAsync(It.IsAny<AutomationRun>())).Returns(Task.CompletedTask);
        state.Setup(s => s.UpdateRunAsync(It.IsAny<AutomationRun>())).Returns(Task.CompletedTask);

        var service = new ArticleAutomationService(
            discovery.Object, ai.Object, articles.Object, articleRepo.Object,
            categories.Object, state.Object, new AutomationCoordinator(),
            NullLogger<ArticleAutomationService>.Instance);

        var run = await service.RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Equal(AutomationRunStatus.Completado, run!.Status);
        Assert.Empty(run.GeneratedArticles);
        Assert.Single(run.PendingDecisions);
        Assert.Equal("Noticia repetida", run.PendingDecisions[0].TopicTitle);
        articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Never);
    }

    [Fact]
    public async Task RunOnceAsync_Schedules_Article_When_ScheduleEnabled()
    {
        var discovery = new Mock<ITopicDiscoveryService>();
        discovery.Setup(d => d.DiscoverAsync(It.IsAny<TopicDiscoveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiscoveredTopic> { new() { Title = "Noticia programada" } });

        var ai = new Mock<IAiArticleGenerator>();
        ai.Setup(a => a.GenerateAsync(It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedArticle
            {
                Title = "Noticia programada",
                Summary = "Resumen",
                Blocks = new List<ContentBlockDto> { new ContentParagraphBlockDto { Text = "Contenido" } }
            });

        ArticleDto? captured = null;
        var articles = new Mock<IArticleService>();
        articles.Setup(a => a.CreateAsync(It.IsAny<ArticleDto>()))
            .Callback<ArticleDto>(dto => captured = dto)
            .ReturnsAsync(new ArticleResponseDto { Id = "id1", Title = "Noticia programada", Status = "Programado" });

        var articleRepo = new Mock<IArticleRepository>();
        articleRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);

        var categories = new Mock<ICategoryService>();
        categories.Setup(c => c.GetAllAsync()).ReturnsAsync(new List<CategoryResponseDto>());

        var state = new Mock<IAutomationStateRepository>();
        state.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["tecnologia"],
                ScheduleEnabled = true,
                PublishHour = 9,
                PublishMinute = 30
            });
        state.Setup(s => s.HashExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        state.Setup(s => s.InsertRunAsync(It.IsAny<AutomationRun>())).Returns(Task.CompletedTask);
        state.Setup(s => s.UpdateRunAsync(It.IsAny<AutomationRun>())).Returns(Task.CompletedTask);

        var service = new ArticleAutomationService(
            discovery.Object, ai.Object, articles.Object, articleRepo.Object,
            categories.Object, state.Object, new AutomationCoordinator(),
            NullLogger<ArticleAutomationService>.Instance);

        var run = await service.RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Single(run!.GeneratedArticles);
        Assert.NotNull(captured);
        Assert.Equal("Programado", captured!.Status);
        Assert.NotNull(captured.ScheduledAt);
    }
}
