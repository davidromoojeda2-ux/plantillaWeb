using Aplication.DTOs;
using Aplication.DTOs.Automation;
using Domine.Entities;

namespace paginaWeb.Areas.Admin.Models;

/// <summary>Modelo del panel principal de automatización.</summary>
public class AutomationIndexViewModel
{
    public AutomationStatusDto Status { get; set; } = new();
    public List<AutomationRun> RecentRuns { get; set; } = [];
    public List<PendingDecisionDto> PendingDecisions { get; set; } = [];
}

/// <summary>Modelo del formulario de configuración del pipeline.</summary>
public class AutomationConfigViewModel
{
    public bool Enabled { get; set; } = true;
    public bool AutoPublish { get; set; }
    public int FrequencyHours { get; set; } = 6;
    public int FrequencyMinutePart { get; set; } = 0;
    public bool RoundRobinTopics { get; set; } = true;
    public string Country { get; set; } = "us";
    public string Language { get; set; } = "es";
    public int MaxArticlesPerRun { get; set; } = 5;
    public int TargetWords { get; set; } = 1000;
    public int MaxTokensPerArticle { get; set; } = 0;

    // Generación avanzada
    public bool AutoStandards { get; set; } = true;
    public string ArticleTemplate { get; set; } = "news";
    public string Tone { get; set; } = "informativo";
    public int MinBlocks { get; set; } = 5;
    public bool IncludeList { get; set; } = true;
    public bool IncludeTable { get; set; }
    public bool IncludeQuote { get; set; }
    public bool IncludeConclusion { get; set; } = true;
    public bool IncludeFaq { get; set; }
    public bool IncludeCta { get; set; }
    public bool IncludeLinks { get; set; }
    public double Temperature { get; set; } = 0.5;
    public int MaxGenerationAttempts { get; set; } = 2;
    public int WordTolerancePercent { get; set; } = 20;
    public string BaseTopicsText { get; set; } = string.Empty;
    public bool DiscoverWithSerper { get; set; } = true;
    public List<string> CategoryIds { get; set; } = [];

    // Claves (se muestran en blanco; en POST, en blanco conserva la existente)
    public string SerperApiKey { get; set; } = string.Empty;
    public string AiApiKey { get; set; } = string.Empty;
    public bool HasSerperKey { get; set; }
    public bool HasAiKey { get; set; }

    // Parámetros de Serper
    public string SerperType { get; set; } = "news";
    public string SerperRecency { get; set; } = "qdr:d";
    public int SerperNum { get; set; } = 10;
    public string SerperSafe { get; set; } = "off";
    public bool SerperAutocorrect { get; set; } = true;

    // Proveedor de IA
    public string AiProvider { get; set; } = "openai";
    public string AiBaseUrl { get; set; } = "https://api.openai.com/v1";
    public string AiModel { get; set; } = "gpt-4o-mini";

    // Programación de publicación
    public bool ScheduleEnabled { get; set; }
    public int PublishHour { get; set; } = 9;
    public int PublishMinute { get; set; } = 0;

    public List<CategoryResponseDto> Categories { get; set; } = [];

    public static AutomationConfigViewModel FromSettings(
        AutomationSettings s, List<CategoryResponseDto> categories)
    {
        var standard = s.AutoStandards ? ArticleGenerationStandards.ForWords(s.TargetWords) : null;

        return new AutomationConfigViewModel
        {
            Enabled = s.Enabled,
            AutoPublish = s.AutoPublish,
            FrequencyHours = s.FrequencyMinutes / 60,
            FrequencyMinutePart = s.FrequencyMinutes % 60,
            RoundRobinTopics = s.RoundRobinTopics,
            Country = s.Country,
            Language = s.Language,
            MaxArticlesPerRun = s.MaxArticlesPerRun,
            TargetWords = s.TargetWords,
            MaxTokensPerArticle = standard?.MaxTokens ?? s.MaxTokensPerArticle,
            AutoStandards = s.AutoStandards,
            ArticleTemplate = s.ArticleTemplate,
            Tone = s.Tone,
            MinBlocks = standard?.MinBlocks ?? s.MinBlocks,
            IncludeList = s.IncludeList,
            IncludeTable = s.IncludeTable,
            IncludeQuote = s.IncludeQuote,
            IncludeConclusion = s.IncludeConclusion,
            IncludeFaq = s.IncludeFaq,
            IncludeCta = s.IncludeCta,
            IncludeLinks = s.IncludeLinks,
            Temperature = s.Temperature,
            MaxGenerationAttempts = s.MaxGenerationAttempts,
            WordTolerancePercent = standard?.WordTolerancePercent ?? s.WordTolerancePercent,
            BaseTopicsText = string.Join(Environment.NewLine, s.BaseTopics),
            DiscoverWithSerper = s.DiscoverWithSerper,
            CategoryIds = s.CategoryIds,
            HasSerperKey = !string.IsNullOrWhiteSpace(s.SerperApiKey),
            HasAiKey = !string.IsNullOrWhiteSpace(s.AiApiKey),
            SerperType = s.SerperType,
            SerperRecency = s.SerperRecency,
            SerperNum = s.SerperNum,
            SerperSafe = s.SerperSafe,
            SerperAutocorrect = s.SerperAutocorrect,
            AiProvider = s.AiProvider,
            AiBaseUrl = s.AiBaseUrl,
            AiModel = s.AiModel,
            ScheduleEnabled = s.ScheduleEnabled,
            PublishHour = s.PublishHour,
            PublishMinute = s.PublishMinute,
            Categories = categories
        };
    }

    public AutomationSettings ToSettings(AutomationSettings existing)
    {
        var targetWords = Math.Clamp(TargetWords, 100, 5000);
        var standard = AutoStandards ? ArticleGenerationStandards.ForWords(targetWords) : null;
        var minBlocks = standard?.MinBlocks ?? Math.Clamp(MinBlocks, 3, 20);
        var maxTokens = standard?.MaxTokens
            ?? (MaxTokensPerArticle <= 0 ? 0 : Math.Clamp(MaxTokensPerArticle, 256, 100000));
        var tolerance = standard?.WordTolerancePercent ?? Math.Clamp(WordTolerancePercent, 0, 80);

        return new AutomationSettings
        {
            Id = existing.Id,
            Enabled = Enabled,
            AutoPublish = AutoPublish,
            Paused = existing.Paused,
            FrequencyMinutes = Math.Max(1, FrequencyHours * 60 + FrequencyMinutePart),
            RoundRobinTopics = RoundRobinTopics,
            Country = string.IsNullOrWhiteSpace(Country) ? "us" : Country.Trim(),
            Language = string.IsNullOrWhiteSpace(Language) ? "es" : Language.Trim(),
            MaxArticlesPerRun = Math.Max(1, MaxArticlesPerRun),
            TargetWords = targetWords,
            MaxTokensPerArticle = maxTokens,
            AutoStandards = AutoStandards,
            ArticleTemplate = string.IsNullOrWhiteSpace(ArticleTemplate) ? "news" : ArticleTemplate.Trim(),
            Tone = string.IsNullOrWhiteSpace(Tone) ? "informativo" : Tone.Trim(),
            MinBlocks = minBlocks,
            IncludeList = IncludeList,
            IncludeTable = IncludeTable,
            IncludeQuote = IncludeQuote,
            IncludeConclusion = IncludeConclusion,
            IncludeFaq = IncludeFaq,
            IncludeCta = IncludeCta,
            IncludeLinks = IncludeLinks,
            Temperature = Math.Clamp(Temperature, 0.0, 1.5),
            MaxGenerationAttempts = Math.Clamp(MaxGenerationAttempts, 1, 5),
            WordTolerancePercent = tolerance,
            BaseTopics = (BaseTopicsText ?? string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
            DiscoverWithSerper = DiscoverWithSerper,
            CategoryIds = CategoryIds ?? [],

            SerperApiKey = string.IsNullOrWhiteSpace(SerperApiKey) ? existing.SerperApiKey : SerperApiKey.Trim(),
            SerperType = string.IsNullOrWhiteSpace(SerperType) ? "news" : SerperType.Trim(),
            SerperRecency = SerperRecency ?? string.Empty,
            SerperNum = Math.Clamp(SerperNum, 1, 100),
            SerperSafe = string.IsNullOrWhiteSpace(SerperSafe) ? "off" : SerperSafe.Trim(),
            SerperAutocorrect = SerperAutocorrect,
            AiApiKey = string.IsNullOrWhiteSpace(AiApiKey) ? existing.AiApiKey : AiApiKey.Trim(),
            AiProvider = string.IsNullOrWhiteSpace(AiProvider) ? "openai" : AiProvider.Trim(),
            AiBaseUrl = string.IsNullOrWhiteSpace(AiBaseUrl) ? existing.AiBaseUrl : AiBaseUrl.Trim(),
            AiModel = string.IsNullOrWhiteSpace(AiModel) ? existing.AiModel : AiModel.Trim(),

            ScheduleEnabled = ScheduleEnabled,
            PublishHour = Math.Clamp(PublishHour, 0, 23),
            PublishMinute = Math.Clamp(PublishMinute, 0, 59),

            CreatedAt = existing.CreatedAt
        };
    }
}
