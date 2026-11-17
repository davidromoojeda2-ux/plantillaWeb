using Aplication.DTOs;
using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;
using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ArticleAutomation.Tests;

/// <summary>
/// Monta el servicio de automatización con colaboradores simulados y valores por
/// defecto válidos. Cada prueba solo ajusta lo que necesita.
/// </summary>
internal sealed class AutomationHarness
{
    private readonly AutomationCoordinator _coordinator = new();
    private int _created;

    public Mock<ITopicDiscoveryService> Discovery { get; } = new();
    public Mock<IAiArticleGenerator> Ai { get; } = new();
    public Mock<ITopicCountryValidator> CountryValidator { get; } = new();
    public Mock<IArticleService> Articles { get; } = new();
    public Mock<IArticleRepository> ArticleRepo { get; } = new();
    public Mock<ICategoryService> Categories { get; } = new();
    public Mock<IAutomationStateRepository> State { get; } = new();

    public AutomationHarness()
    {
        CountryValidator
            .Setup(v => v.ValidateAsync(It.IsAny<TopicCountryCheckRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TopicCountryCheckResult(true));

        Ai.Setup(a => a.GenerateAsync(It.IsAny<TopicGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TopicGenerationRequest request, CancellationToken _) => TestArticles.Valid(request.TopicTitle));

        Articles.Setup(a => a.CreateAsync(It.IsAny<ArticleDto>()))
            .ReturnsAsync((ArticleDto dto) => new ArticleResponseDto
            {
                Id = $"id{++_created}",
                Title = dto.Title,
                Status = "Borrador"
            });

        ArticleRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);
        Categories.Setup(c => c.GetAllAsync()).ReturnsAsync([]);

        State.Setup(s => s.HashExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        State.Setup(s => s.InsertRunAsync(It.IsAny<AutomationRun>())).Returns(Task.CompletedTask);
        State.Setup(s => s.UpdateRunAsync(It.IsAny<AutomationRun>())).Returns(Task.CompletedTask);

        WithSettings(DefaultSettings);
        WithDiscovery([new DiscoveredTopic { Title = "Noticia de prueba", Source = "fuente" }]);
    }

    public static AutomationSettings DefaultSettings => new()
    {
        Enabled = true,
        BaseTopics = ["tecnologia"],
        AutoStandards = false
    };

    public AutomationHarness WithSettings(AutomationSettings settings)
    {
        State.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);
        return this;
    }

    public AutomationHarness WithDiscovery(
        IReadOnlyList<DiscoveredTopic> topics,
        Action<TopicDiscoveryRequest>? onRequest = null)
    {
        var setup = Discovery.Setup(d => d.DiscoverAsync(
            It.IsAny<TopicDiscoveryRequest>(), It.IsAny<CancellationToken>()));

        if (onRequest is not null)
        {
            setup.Callback<TopicDiscoveryRequest, CancellationToken>((request, _) => onRequest(request));
        }

        setup.ReturnsAsync(new DiscoveryResult { Topics = [.. topics] });
        return this;
    }

    public ArticleAutomationService CreateService() => new(
        Discovery.Object,
        Ai.Object,
        CountryValidator.Object,
        Articles.Object,
        ArticleRepo.Object,
        Categories.Object,
        State.Object,
        _coordinator,
        NullLogger<ArticleAutomationService>.Instance);
}

/// <summary>Artículos generados de ejemplo compartidos por las pruebas.</summary>
internal static class TestArticles
{
    /// <summary>Artículo que no cumple la estructura mínima.</summary>
    public static GeneratedArticle Short(string title) => new()
    {
        Title = title,
        Summary = "Resumen",
        MetaDescription = "Meta",
        Tags = ["uno"],
        Blocks = [new ContentParagraphBlockDto { Text = "Texto corto." }]
    };

    /// <summary>Artículo que cumple la validación de estructura por defecto.</summary>
    public static GeneratedArticle Valid(string title, int words = 820)
    {
        var filler = string.Join(' ', Enumerable.Repeat("contenido", words));

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
