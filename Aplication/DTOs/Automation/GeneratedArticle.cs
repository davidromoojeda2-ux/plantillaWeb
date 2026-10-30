using Aplication.DTOs.ContentBlocks;

namespace Aplication.DTOs.Automation;

/// <summary>Artículo generado por la IA, listo para validar y persistir.</summary>
public sealed class GeneratedArticle
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<ContentBlockDto> Blocks { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public string? CategorySuggestion { get; set; }
    public string MetaDescription { get; set; } = string.Empty;
}
