using Aplication.DTOs.Automation;

namespace Aplication.Interfaces;

/// <summary>Solicitud para comprobar si un tema descubierto corresponde al país configurado.</summary>
public sealed class TopicCountryCheckRequest
{
    public string TopicTitle { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public string? Source { get; set; }

    /// <summary>Código de país (gl) configurado para Serper.</summary>
    public string CountryCode { get; set; } = "us";

    /// <summary>Idioma de la respuesta (motivo).</summary>
    public string Language { get; set; } = "es";

    /// <summary>Clave del proveedor de IA, provista por el orquestador.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>URL base del endpoint compatible con OpenAI.</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>Modelo a utilizar.</summary>
    public string Model { get; set; } = "gpt-4o-mini";
}

/// <summary>Resultado de la comprobación de país de un tema.</summary>
public sealed record TopicCountryCheckResult(
    bool IsRelevant,
    string? Reason = null,
    int? TotalTokens = null);

/// <summary>
/// Comprueba si un tema descubierto es relevante para el país configurado antes de
/// redactar el artículo. Evita publicar temas que Serper devolvió desde otra región.
/// </summary>
public interface ITopicCountryValidator
{
    Task<TopicCountryCheckResult> ValidateAsync(
        TopicCountryCheckRequest request, CancellationToken ct = default);
}
