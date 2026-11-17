using System.Text.Json;
using Aplication.DTOs;
using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Moq;

namespace ArticleAutomation.Tests;

public class ArticleAutomationServiceTests
{
    [Fact]
    public async Task RunOnceAsync_Generates_And_Creates_Article()
    {
        var harness = new AutomationHarness();

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Equal(AutomationRunStatus.Completado, run!.Status);
        Assert.Single(run.GeneratedArticles);
        Assert.Equal("id1", run.GeneratedArticles[0].ArticleId);
        harness.Articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Once);
    }

    [Fact]
    public async Task RunOnceAsync_RoundRobin_Generates_One_Article_Per_Base_Topic()
    {
        TopicDiscoveryRequest? captured = null;
        var harness = new AutomationHarness()
            .WithDiscovery(
                [
                    new DiscoveredTopic { Title = "Kit solar portátil", BaseTopic = "Kit solar" },
                    new DiscoveredTopic { Title = "Inversor híbrido 5 kW", BaseTopic = "Inversor híbrido" },
                    new DiscoveredTopic { Title = "Placa solar 450 W", BaseTopic = "Energía de placa 450 W" }
                ],
                request => captured = request)
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["Kit solar", "Inversor híbrido", "Energía de placa 450 W"],
                DiscoverWithSerper = true,
                RoundRobinTopics = true,
                MaxArticlesPerRun = 1,
                Country = "mx",
                AutoStandards = false
            });

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Equal(
            ["Kit solar portátil", "Inversor híbrido 5 kW", "Placa solar 450 W"],
            run!.GeneratedArticles.Select(a => a.Title).ToArray());
        Assert.NotNull(captured);
        Assert.Equal("mx", captured!.Country);
        Assert.True(captured.Limit >= 6);
        harness.Articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Exactly(3));
    }

    [Fact]
    public async Task RunOnceAsync_Without_RoundRobin_Respects_MaxArticlesPerRun()
    {
        var harness = new AutomationHarness()
            .WithDiscovery(
                [
                    new DiscoveredTopic { Title = "Tema A", BaseTopic = "a" },
                    new DiscoveredTopic { Title = "Tema B", BaseTopic = "b" },
                    new DiscoveredTopic { Title = "Tema C", BaseTopic = "c" }
                ])
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["a", "b", "c"],
                DiscoverWithSerper = true,
                RoundRobinTopics = false,
                MaxArticlesPerRun = 1,
                AutoStandards = false
            });

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Single(run!.GeneratedArticles);
        Assert.Equal("Tema A", run.GeneratedArticles[0].Title);
    }

    [Fact]
    public async Task RunOnceAsync_RoundRobin_Skips_Duplicate_And_Covers_Remaining_Topics()
    {
        const string duplicateTitle = "Kit solar repetido";
        var duplicateHash = SlugGenerator.Hash(duplicateTitle);

        var harness = new AutomationHarness()
            .WithDiscovery(
                [
                    new DiscoveredTopic { Title = duplicateTitle, BaseTopic = "Kit solar" },
                    new DiscoveredTopic { Title = "Inversor híbrido nuevo", BaseTopic = "Inversor híbrido" },
                    new DiscoveredTopic { Title = "Placa solar 500 W", BaseTopic = "Placa solar" },
                    new DiscoveredTopic { Title = "Kit solar de respaldo", BaseTopic = "Kit solar" }
                ])
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["Kit solar", "Inversor híbrido", "Placa solar"],
                DiscoverWithSerper = true,
                RoundRobinTopics = true,
                MaxArticlesPerRun = 1,
                AutoStandards = false
            });

        harness.State.Setup(s => s.HashExistsAsync(It.IsAny<string>()))
            .ReturnsAsync((string hash) => hash == duplicateHash);

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Equal(
            ["Kit solar de respaldo", "Inversor híbrido nuevo", "Placa solar 500 W"],
            run!.GeneratedArticles.Select(a => a.Title).ToArray());
        Assert.Contains(run.PendingDecisions, d => d.TopicTitle == duplicateTitle);
    }

    [Fact]
    public async Task RunOnceAsync_Duplicate_Topic_Is_Flagged_For_Decision()
    {
        var harness = new AutomationHarness()
            .WithDiscovery([new DiscoveredTopic { Title = "Noticia repetida" }]);

        harness.ArticleRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Equal(AutomationRunStatus.Completado, run!.Status);
        Assert.Empty(run.GeneratedArticles);
        Assert.Single(run.PendingDecisions);
        Assert.Equal("Noticia repetida", run.PendingDecisions[0].TopicTitle);
        harness.Articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Never);
    }

    [Fact]
    public async Task RunOnceAsync_Schedules_Article_When_ScheduleEnabled()
    {
        ArticleDto? captured = null;

        var harness = new AutomationHarness()
            .WithDiscovery([new DiscoveredTopic { Title = "Noticia programada" }])
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["tecnologia"],
                AutoStandards = false,
                ScheduleEnabled = true,
                PublishHour = 9,
                PublishMinute = 30
            });

        harness.Articles.Setup(a => a.CreateAsync(It.IsAny<ArticleDto>()))
            .Callback<ArticleDto>(dto => captured = dto)
            .ReturnsAsync(new ArticleResponseDto { Id = "id1", Title = "Noticia programada", Status = "Programado" });

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Single(run!.GeneratedArticles);
        Assert.NotNull(captured);
        Assert.Equal("Programado", captured!.Status);
        Assert.NotNull(captured.ScheduledAt);
    }

    [Fact]
    public async Task RunOnceAsync_Retries_With_Correction_Hint_When_Invalid()
    {
        var requests = new List<TopicGenerationRequest>();
        var calls = 0;

        var harness = new AutomationHarness()
            .WithDiscovery([new DiscoveredTopic { Title = "Noticia con reintento" }])
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["tecnologia"],
                AutoStandards = false,
                MaxGenerationAttempts = 2
            });

        harness.Ai.Setup(a => a.GenerateAsync(It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TopicGenerationRequest, CancellationToken>((request, _) => requests.Add(request))
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
                    : TestArticles.Valid("Noticia con reintento");
            });

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Single(run!.GeneratedArticles);
        Assert.Empty(run.Errors);
        Assert.Equal(2, calls);
        Assert.NotNull(requests[1].CorrectionHint);
        harness.Articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Once);
    }

    [Fact]
    public async Task RunOnceAsync_Invalid_Article_Is_Pending_Confirmation()
    {
        var harness = new AutomationHarness()
            .WithDiscovery([new DiscoveredTopic { Title = "Noticia fuera de estándar" }])
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["tecnologia"],
                AutoStandards = false,
                MaxGenerationAttempts = 1,
                TargetWords = 1000,
                MinBlocks = 5,
                WordTolerancePercent = 20
            });

        harness.Ai.Setup(a => a.GenerateAsync(It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestArticles.Short("Noticia fuera de estándar"));

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Empty(run!.GeneratedArticles);
        Assert.Empty(run.Errors);
        var decision = Assert.Single(run.PendingDecisions);
        Assert.Equal(AutomationDecisionKind.Validacion, decision.Kind);
        Assert.NotEmpty(decision.ValidationErrors);
        Assert.False(string.IsNullOrWhiteSpace(decision.DraftJson));
        harness.Articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Never);
    }

    [Fact]
    public async Task ResolveDecisionAsync_Saves_Validation_Draft_When_Confirmed()
    {
        var draft = TestArticles.Short("Artículo fuera de estándar");
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

        var harness = new AutomationHarness();
        harness.State.Setup(s => s.GetRunAsync("run1")).ReturnsAsync(run);

        await harness.CreateService().ResolveDecisionAsync("run1", "dec1", continueCreating: true);

        Assert.Single(run.GeneratedArticles);
        Assert.Equal(AutomationDecisionKind.Validacion, decision.Kind);
        Assert.Equal(AutomationDecisionStatus.Continuado, decision.Status);
        Assert.Null(decision.DraftJson);
        harness.Articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Once);
        harness.Ai.Verify(a => a.GenerateAsync(
            It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveDecisionAsync_Discards_Validation_Draft_When_Omitted()
    {
        var draft = TestArticles.Short("Artículo descartado");
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

        var harness = new AutomationHarness();
        harness.State.Setup(s => s.GetRunAsync("run2")).ReturnsAsync(run);

        await harness.CreateService().ResolveDecisionAsync("run2", "dec2", continueCreating: false);

        Assert.Equal(AutomationDecisionKind.Validacion, decision.Kind);
        Assert.Equal(AutomationDecisionStatus.Omitido, decision.Status);
        Assert.Null(decision.DraftJson);
        harness.Articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Never);
    }

    [Fact]
    public async Task RunOnceAsync_Omits_Whole_Topic_When_Country_Validation_Rejects_It()
    {
        var harness = new AutomationHarness()
            .WithDiscovery(
                [
                    new DiscoveredTopic { Title = "Noticia local de México", BaseTopic = "local" },
                    new DiscoveredTopic { Title = "Otra noticia local de México", BaseTopic = "local" },
                    new DiscoveredTopic { Title = "Noticia internacional", BaseTopic = "internacional" }
                ])
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["local", "internacional"],
                DiscoverWithSerper = true,
                RoundRobinTopics = true,
                MaxArticlesPerRun = 1,
                Country = "mx",
                AutoStandards = false
            });

        harness.CountryValidator
            .Setup(v => v.ValidateAsync(It.IsAny<TopicCountryCheckRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TopicCountryCheckRequest request, CancellationToken _) =>
                request.TopicTitle == "Noticia local de México"
                    ? new TopicCountryCheckResult(false, "El tema pertenece a otro país.", 42)
                    : new TopicCountryCheckResult(true));

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Equal(["Noticia internacional"], run!.GeneratedArticles.Select(a => a.Title).ToArray());
        var skipped = Assert.Single(run.SkippedTopics);
        Assert.Equal("Noticia local de México", skipped.TopicTitle);
        Assert.Equal("local", skipped.BaseTopic);
        Assert.Contains("otro país", skipped.Reason);
        Assert.Equal(42, run.TotalTokens);
        harness.Ai.Verify(a => a.GenerateAsync(
            It.Is<TopicGenerationRequest>(r => r.TopicTitle == "Otra noticia local de México"),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunOnceAsync_Omits_Topic_When_Country_Validation_Fails()
    {
        var harness = new AutomationHarness()
            .WithDiscovery([new DiscoveredTopic { Title = "Noticia sin validar" }]);

        harness.CountryValidator
            .Setup(v => v.ValidateAsync(It.IsAny<TopicCountryCheckRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sin conexión"));

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Empty(run!.GeneratedArticles);
        Assert.Empty(run.PendingDecisions);
        var skipped = Assert.Single(run.SkippedTopics);
        Assert.Equal("Noticia sin validar", skipped.TopicTitle);
        Assert.Contains("sin conexión", skipped.Reason);
        harness.Ai.Verify(a => a.GenerateAsync(
            It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        harness.Articles.Verify(a => a.CreateAsync(It.IsAny<ArticleDto>()), Times.Never);
    }

    [Fact]
    public async Task RunOnceAsync_Does_Not_Validate_Country_When_Serper_Is_Disabled()
    {
        var harness = new AutomationHarness()
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["tecnologia"],
                DiscoverWithSerper = false,
                AutoStandards = false
            });

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Single(run!.GeneratedArticles);
        Assert.Empty(run.SkippedTopics);
        harness.CountryValidator.Verify(v => v.ValidateAsync(
            It.IsAny<TopicCountryCheckRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        harness.Discovery.Verify(d => d.DiscoverAsync(
            It.IsAny<TopicDiscoveryRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunOnceAsync_Without_RoundRobin_Skips_Only_Rejected_Topic()
    {
        var harness = new AutomationHarness()
            .WithDiscovery(
                [
                    new DiscoveredTopic { Title = "Tema de otro país", BaseTopic = "a" },
                    new DiscoveredTopic { Title = "Tema del país", BaseTopic = "b" }
                ])
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["a", "b"],
                DiscoverWithSerper = true,
                RoundRobinTopics = false,
                MaxArticlesPerRun = 5,
                AutoStandards = false
            });

        harness.CountryValidator
            .Setup(v => v.ValidateAsync(It.IsAny<TopicCountryCheckRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TopicCountryCheckRequest request, CancellationToken _) =>
                request.TopicTitle == "Tema de otro país"
                    ? new TopicCountryCheckResult(false, "Otro país.")
                    : new TopicCountryCheckResult(true));

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Equal(["Tema del país"], run!.GeneratedArticles.Select(a => a.Title).ToArray());
        var skipped = Assert.Single(run.SkippedTopics);
        Assert.Equal("Tema de otro país", skipped.TopicTitle);
    }

    [Fact]
    public async Task RunOnceAsync_Removes_Unsolicited_Conclusion_And_Faq_Before_Creating_Article()
    {
        ArticleDto? captured = null;

        var harness = new AutomationHarness()
            .WithDiscovery([new DiscoveredTopic { Title = "tecnologia" }])
            .WithSettings(new AutomationSettings
            {
                Enabled = true,
                BaseTopics = ["tecnologia"],
                DiscoverWithSerper = false,
                IncludeConclusion = false,
                IncludeFaq = false,
                MinBlocks = 3,
                MaxGenerationAttempts = 1,
                AutoStandards = false
            });

        harness.Ai.Setup(a => a.GenerateAsync(It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var article = TestArticles.Valid("Tema de prueba");
                article.Blocks.Add(new ContentHeadingBlockDto { Level = 2, Text = "Preguntas frecuentes" });
                article.Blocks.Add(new ContentHeadingBlockDto { Level = 3, Text = "¿Pregunta?" });
                article.Blocks.Add(new ContentParagraphBlockDto { Text = "Respuesta." });
                return article;
            });

        harness.Articles.Setup(a => a.CreateAsync(It.IsAny<ArticleDto>()))
            .Callback<ArticleDto>(dto => captured = dto)
            .ReturnsAsync(new ArticleResponseDto { Id = "id1", Title = "Tema de prueba", Status = "Borrador" });

        var run = await harness.CreateService().RunOnceAsync(AutomationTrigger.Manual, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Single(run!.GeneratedArticles);
        Assert.NotNull(captured);
        Assert.DoesNotContain(captured!.Blocks, b =>
            b is ContentHeadingBlockDto h &&
            (h.Text.Contains("Conclusión") || h.Text.Contains("Preguntas frecuentes")));
        Assert.Contains(captured.Blocks, b => b is ContentBulletListBlockDto);
    }
}
