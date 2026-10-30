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
