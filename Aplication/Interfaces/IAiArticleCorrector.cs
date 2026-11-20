using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;
using Domine.Entities;

namespace Aplication.Interfaces;

/// <summary>
/// Solicitud para corregir un artículo existente aplicando las observaciones de la
/// validación. A diferencia de una reescritura completa, solo se toca lo señalado.
/// </summary>
public sealed class ArticleCorrectionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<ContentBlockDto> Blocks { get; set; } = [];

    public string Language { get; set; } = "es";
    public string Template { get; set; } = "news";

    /// <summary>Umbrales de párrafo por plantilla definidos en la configuración.</summary>
    public IReadOnlyDictionary<string, ParagraphStyleSetting>? ParagraphStyles { get; set; }

    /// <summary>Observaciones que la IA debe corregir, en el orden mostrado al usuario.</summary>
    public List<string> Issues { get; set; } = [];

    /// <summary>Enlaces internos con los que sustituir enlaces caídos.</summary>
    public List<InternalLinkCandidate> InternalLinks { get; set; } = [];

    public double Temperature { get; set; } = 0.3;
    public int MaxTokens { get; set; } = 4000;

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
}

/// <summary>Corrige un artículo con IA a partir de las observaciones de validación.</summary>
public interface IAiArticleCorrector
{
    Task<GeneratedArticle> CorrectAsync(ArticleCorrectionRequest request, CancellationToken ct = default);
}
