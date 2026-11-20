using Aplication.DTOs.ContentBlocks;

namespace Aplication.DTOs.Automation;

/// <summary>Acción de enlace propuesta por la IA para que el usuario la confirme.</summary>
public sealed record ArticleLinkAction(
    string Type,
    string Url,
    string? Replacement = null,
    string? Reason = null);

/// <summary>Artículo generado por la IA, listo para validar y persistir.</summary>
public sealed class GeneratedArticle
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<ContentBlockDto> Blocks { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public string? CategorySuggestion { get; set; }
    public string MetaDescription { get; set; } = string.Empty;

    /// <summary>
    /// Cambios de enlaces propuestos por el corrector. No se aplican hasta que el
    /// usuario los confirma y el servidor vuelve a validar las URLs.
    /// </summary>
    public List<ArticleLinkAction> LinkActions { get; set; } = [];

    /// <summary>Tokens totales consumidos por la generación (si el proveedor los informa).</summary>
    public int? TotalTokens { get; set; }
}
