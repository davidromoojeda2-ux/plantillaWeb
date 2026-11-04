namespace Aplication.DTOs.Automation;

/// <summary>Artículo existente que la IA puede enlazar internamente en el cuerpo.</summary>
public sealed class InternalLinkCandidate
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
