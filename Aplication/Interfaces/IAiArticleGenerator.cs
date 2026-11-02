using Aplication.DTOs.Automation;

namespace Aplication.Interfaces;

/// <summary>Solicitud de generación de un artículo a partir de una temática.</summary>
public sealed class TopicGenerationRequest
{
    public string TopicTitle { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public string? Source { get; set; }
    public string Language { get; set; } = "es";
    public int MaxTokens { get; set; } = 2000;

    /// <summary>Cantidad de palabras deseada para el artículo (se usa en el prompt).</summary>
    public int TargetWords { get; set; } = 1000;

    /// <summary>Plantilla de estructura: "news", "informative", "analysis" o "guide".</summary>
    public string Template { get; set; } = "news";

    /// <summary>Tono editorial: "informativo", "analitico" o "cronica".</summary>
    public string Tone { get; set; } = "informativo";

    public int MinBlocks { get; set; } = 5;
    public int MaxBlocks { get; set; } = 10;
    public bool IncludeList { get; set; } = true;
    public bool IncludeTable { get; set; }
    public bool IncludeQuote { get; set; }
    public bool IncludeConclusion { get; set; } = true;

    /// <summary>Temperatura del modelo (0.0–1.0).</summary>
    public double Temperature { get; set; } = 0.5;

    /// <summary>Indicaciones de corrección cuando el intento anterior no cumplió la estructura.</summary>
    public string? CorrectionHint { get; set; }

    /// <summary>Clave del proveedor de IA, provista por el orquestador.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>URL base del endpoint compatible con OpenAI.</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>Modelo a utilizar.</summary>
    public string Model { get; set; } = "gpt-4o-mini";
}

/// <summary>Genera un artículo estructurado a partir de una temática.</summary>
public interface IAiArticleGenerator
{
    Task<GeneratedArticle> GenerateAsync(TopicGenerationRequest request, CancellationToken ct = default);
}
