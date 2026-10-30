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
    public bool Paused { get; set; }
    public int FrequencyMinutes { get; set; } = 360;
    public string Country { get; set; } = "us";
    public string Language { get; set; } = "es";
    public int MaxArticlesPerRun { get; set; } = 5;
    public int MaxTokensPerArticle { get; set; } = 8000;
    public string BaseTopicsText { get; set; } = string.Empty;
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
        AutomationSettings s, List<CategoryResponseDto> categories) => new()
    {
        Enabled = s.Enabled,
        AutoPublish = s.AutoPublish,
        Paused = s.Paused,
        FrequencyMinutes = s.FrequencyMinutes,
        Country = s.Country,
        Language = s.Language,
        MaxArticlesPerRun = s.MaxArticlesPerRun,
        MaxTokensPerArticle = s.MaxTokensPerArticle,
        BaseTopicsText = string.Join(Environment.NewLine, s.BaseTopics),
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

    public AutomationSettings ToSettings(AutomationSettings existing) => new()
    {
        Id = existing.Id,
        Enabled = Enabled,
        AutoPublish = AutoPublish,
        Paused = Paused,
        FrequencyMinutes = Math.Max(1, FrequencyMinutes),
        Country = string.IsNullOrWhiteSpace(Country) ? "us" : Country.Trim(),
        Language = string.IsNullOrWhiteSpace(Language) ? "es" : Language.Trim(),
        MaxArticlesPerRun = Math.Max(1, MaxArticlesPerRun),
        MaxTokensPerArticle = Math.Max(256, MaxTokensPerArticle),
        BaseTopics = (BaseTopicsText ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList(),
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
