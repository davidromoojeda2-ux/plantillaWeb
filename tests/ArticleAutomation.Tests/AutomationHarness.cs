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

    /// <summary>Artículo que cumple la validación de estructura y estilo por defecto.</summary>
    public static GeneratedArticle Valid(string title, int words = 820)
    {
        var blocks = new List<ContentBlockDto>();
        var remaining = Math.Max(words, 1);
        var sections = new[] { "Desarrollo", "Contexto", "Detalles", "Aplicaciones" };

        var intro = Math.Min(remaining, 60);
        blocks.Add(Paragraph(intro));
        remaining -= intro;

        var sectionIndex = 0;
        while (remaining > 0)
        {
            blocks.Add(new ContentHeadingBlockDto { Level = 2, Text = sections[sectionIndex % sections.Length] });

            for (var i = 0; i < 2 && remaining > 0; i++)
            {
                var take = Math.Min(60, remaining);
                blocks.Add(Paragraph(take));
                remaining -= take;
            }

            if (sectionIndex == 0)
                blocks.Add(new ContentBulletListBlockDto { Items = ["Primer punto", "Segundo punto", "Tercer punto"] });

            sectionIndex++;
        }

        return new GeneratedArticle
        {
            Title = title,
            Summary = "Resumen de prueba del artículo generado.",
            MetaDescription = "Meta descripción de prueba con la longitud necesaria para validar el artículo generado.",
            Tags = ["uno", "dos", "tres"],
            Blocks = blocks
        };
    }

    private static ContentParagraphBlockDto Paragraph(int words)
    {
        var filler = string.Join(' ', Enumerable.Repeat("contenido", Math.Max(0, words - 3)));
        return new ContentParagraphBlockDto { Text = $"Contenido de prueba. {filler}".Trim() };
    }
}
