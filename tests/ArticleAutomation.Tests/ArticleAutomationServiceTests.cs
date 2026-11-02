using System.Text.Json;
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
            .ReturnsAsync(new DiscoveryResult { Topics = [new DiscoveredTopic { Title = "Noticia de prueba", Source = "fuente" }] });

        var ai = new Mock<IAiArticleGenerator>();
        ai.Setup(a => a.GenerateAsync(It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildValidArticle("Noticia de prueba"));

        var articles = new Mock<IArticleService>();
        articles.Setup(a => a.CreateAsync(It.IsAny<ArticleDto>()))
            .ReturnsAsync(new ArticleResponseDto { Id = "id1", Title = "Noticia de prueba", Status = "Borrador" });

        var articleRepo = new Mock<IArticleRepository>();
        articleRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);

        var categories = new Mock<ICategoryService>();
        categories.Setup(c => c.GetAllAsync()).ReturnsAsync(new List<CategoryResponseDto>());

        var state = new Mock<IAutomationStateRepository>();
        state.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new AutomationSettings { Enabled = true, BaseTopics = ["tecnologia"], AutoStandards = false });
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
            .ReturnsAsync(new DiscoveryResult { Topics = [new DiscoveredTopic { Title = "Noticia repetida" }] });

        var ai = new Mock<IAiArticleGenerator>();
        var articles = new Mock<IArticleService>();

        var articleRepo = new Mock<IArticleRepository>();
        articleRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(true);

        var categories = new Mock<ICategoryService>();
        categories.Setup(c => c.GetAllAsync()).ReturnsAsync(new List<CategoryResponseDto>());

        var state = new Mock<IAutomationStateRepository>();
        state.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new AutomationSettings { Enabled = true, BaseTopics = ["tecnologia"], AutoStandards = false });
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
            .ReturnsAsync(new DiscoveryResult { Topics = [new DiscoveredTopic { Title = "Noticia programada" }] });

        var ai = new Mock<IAiArticleGenerator>();
        ai.Setup(a => a.GenerateAsync(It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildValidArticle("Noticia programada"));

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
                AutoStandards = false,
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

    [Fact]
    public async Task RunOnceAsync_Retries_With_Correction_Hint_When_Invalid()
    {
        var discovery = new Mock<ITopicDiscoveryService>();
        discovery.Setup(d => d.DiscoverAsync(It.IsAny<TopicDiscoveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DiscoveryResult { Topics = [new DiscoveredTopic { Title = "Noticia con reintento" }] });

        var requests = new List<TopicGenerationRequest>();
        var calls = 0;
        var ai = new Mock<IAiArticleGenerator>();
        ai.Setup(a => a.GenerateAsync(It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TopicGenerationRequest, CancellationToken>((r, _) => requests.Add(r))
            .ReturnsAsync(() =>
            {
                calls++;
                return calls == 1
                    ? new GeneratedArticle
                    {
                        Title = "Noticia incompleta",
                        Summary = "Resumen",
                        Blocks = [new ContentParagraphBlockDto { Text = "Contenido corto." }],
                        Tags = ["uno"]
                    }
                    : BuildValidArticle("Noticia con reintento");
            });

        var articles = new Mock<IArticleService>();
        articles.Setup(a => a.CreateAsync(It.IsAny<ArticleDto>()))
            .ReturnsAsync(new ArticleResponseDto { Id = "id2", Title = "Noticia con reintento", Status = "Borrador" });

        var articleRepo = new Mock<IArticleRepository>();
        articleRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);

        var categories = new Mock<ICategoryService>();
        categories.Setup(c => c.GetAllAsync()).ReturnsAsync(new List<CategoryResponseDto>());

        var state = new Mock<IAutomationStateRepository>();
        state.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new AutomationSettings { Enabled = true, BaseTopics = ["tecnologia"], AutoStandards = false, MaxGenerationAttempts = 2 });
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
        Assert.Empty(run.Errors);
        Assert.Equal(2, calls);
        Assert.NotNull(requests[1].CorrectionHint);
        articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Once);
    }

    [Fact]
    public async Task RunOnceAsync_Invalid_Article_Is_Pending_Confirmation()
    {
        var discovery = new Mock<ITopicDiscoveryService>();
        discovery.Setup(d => d.DiscoverAsync(It.IsAny<TopicDiscoveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DiscoveryResult { Topics = [new DiscoveredTopic { Title = "Noticia fuera de estándar" }] });

        var ai = new Mock<IAiArticleGenerator>();
        ai.Setup(a => a.GenerateAsync(It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildShortArticle("Noticia fuera de estándar"));

        var articles = new Mock<IArticleService>();

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
                AutoStandards = false,
                MaxGenerationAttempts = 1,
                TargetWords = 1000,
                MinBlocks = 5,
                MaxBlocks = 10,
                WordTolerancePercent = 20
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
        Assert.Empty(run!.GeneratedArticles);
        Assert.Empty(run.Errors);
        var decision = Assert.Single(run.PendingDecisions);
        Assert.Equal(AutomationDecisionKind.Validacion, decision.Kind);
        Assert.NotEmpty(decision.ValidationErrors);
        Assert.False(string.IsNullOrWhiteSpace(decision.DraftJson));
        articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Never);
    }

    [Fact]
    public async Task ResolveDecisionAsync_Saves_Validation_Draft_When_Confirmed()
    {
        var draft = BuildShortArticle("Artículo fuera de estándar");
        var decision = new AutomationDecision
        {
            Id = "dec1",
            TopicTitle = draft.Title,
            Kind = AutomationDecisionKind.Validacion,
            ValidationErrors = ["Texto demasiado corto."],
            DraftJson = JsonSerializer.Serialize(draft)
        };
        var run = new AutomationRun
        {
            Id = "run1",
            PendingDecisions = [decision]
        };

        var ai = new Mock<IAiArticleGenerator>();
        var articles = new Mock<IArticleService>();
        articles.Setup(a => a.CreateAsync(It.IsAny<ArticleDto>()))
            .ReturnsAsync(new ArticleResponseDto { Id = "id1", Title = draft.Title, Status = "Borrador" });

        var categories = new Mock<ICategoryService>();
        categories.Setup(c => c.GetAllAsync()).ReturnsAsync(new List<CategoryResponseDto>());

        var state = new Mock<IAutomationStateRepository>();
        state.Setup(s => s.GetRunAsync("run1")).ReturnsAsync(run);
        state.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new AutomationSettings { AutoStandards = false });
        state.Setup(s => s.UpdateRunAsync(It.IsAny<AutomationRun>())).Returns(Task.CompletedTask);

        var service = new ArticleAutomationService(
            new Mock<ITopicDiscoveryService>().Object, ai.Object, articles.Object,
            new Mock<IArticleRepository>().Object, categories.Object, state.Object,
            new AutomationCoordinator(), NullLogger<ArticleAutomationService>.Instance);

        await service.ResolveDecisionAsync("run1", "dec1", continueCreating: true);

        Assert.Single(run.GeneratedArticles);
        Assert.Equal(AutomationDecisionKind.Validacion, decision.Kind);
        Assert.Equal(AutomationDecisionStatus.Continuado, decision.Status);
        Assert.Null(decision.DraftJson);
        articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Once);
        ai.Verify(a => a.GenerateAsync(It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveDecisionAsync_Discards_Validation_Draft_When_Omitted()
    {
        var draft = BuildShortArticle("Artículo descartado");
        var decision = new AutomationDecision
        {
            Id = "dec2",
            TopicTitle = draft.Title,
            Kind = AutomationDecisionKind.Validacion,
            ValidationErrors = ["Texto demasiado corto."],
            DraftJson = JsonSerializer.Serialize(draft)
        };
        var run = new AutomationRun
        {
            Id = "run2",
            PendingDecisions = [decision]
        };

        var articles = new Mock<IArticleService>();
        var state = new Mock<IAutomationStateRepository>();
        state.Setup(s => s.GetRunAsync("run2")).ReturnsAsync(run);
        state.Setup(s => s.UpdateRunAsync(It.IsAny<AutomationRun>())).Returns(Task.CompletedTask);

        var service = new ArticleAutomationService(
            new Mock<ITopicDiscoveryService>().Object, new Mock<IAiArticleGenerator>().Object,
            articles.Object, new Mock<IArticleRepository>().Object, new Mock<ICategoryService>().Object,
            state.Object, new AutomationCoordinator(), NullLogger<ArticleAutomationService>.Instance);

        await service.ResolveDecisionAsync("run2", "dec2", continueCreating: false);

        Assert.Equal(AutomationDecisionKind.Validacion, decision.Kind);
        Assert.Equal(AutomationDecisionStatus.Omitido, decision.Status);
        Assert.Null(decision.DraftJson);
        articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Never);
    }

    /// <summary>Artículo de prueba que no cumple la estructura mínima.</summary>
    private static GeneratedArticle BuildShortArticle(string title) => new()
    {
        Title = title,
        Summary = "Resumen",
        MetaDescription = "Meta",
        Tags = ["uno"],
        Blocks = [new ContentParagraphBlockDto { Text = "Texto corto." }]
    };

    /// <summary>Artículo de prueba que cumple la validación de estructura por defecto.</summary>
    private static GeneratedArticle BuildValidArticle(string title)
    {
        var filler = string.Join(' ', Enumerable.Repeat("contenido", 820));

        return new GeneratedArticle
        {
            Title = title,
            Summary = "Resumen de prueba del artículo generado.",
            MetaDescription = "Meta descripción de prueba con la longitud necesaria para validar el artículo generado.",
            Tags = ["uno", "dos", "tres"],
            Blocks =
            [
                new ContentParagraphBlockDto { Text = $"Introducción del artículo. {filler}" },
                new ContentHeadingBlockDto { Level = 2, Text = "Desarrollo" },
                new ContentParagraphBlockDto { Text = "Desarrollo del tema con detalle y contexto." },
                new ContentBulletListBlockDto { Items = ["Primer punto", "Segundo punto", "Tercer punto"] },
                new ContentHeadingBlockDto { Level = 2, Text = "Conclusión" },
                new ContentParagraphBlockDto { Text = "Cierre del artículo con una idea final." }
            ]
        };
    }
}
