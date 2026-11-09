using Aplication.DTOs.Automation;
using Aplication.DTOs.ContentBlocks;

namespace Aplication.Interfaces;

/// <summary>Solicitud para reescribir un artículo existente con IA.</summary>
public sealed class ArticleRewriteRequest
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<ContentBlockDto> Blocks { get; set; } = [];

    public string Language { get; set; } = "es";
    public string Tone { get; set; } = "informativo";
    public string Template { get; set; } = "news";

    public int TargetWords { get; set; } = 1000;
    public int MinBlocks { get; set; } = 5;
    public int MaxTokens { get; set; } = 4000;

    public bool IncludeList { get; set; } = true;
    public bool IncludeTable { get; set; }
    public bool IncludeQuote { get; set; }
    public bool IncludeConclusion { get; set; } = true;
    public bool IncludeFaq { get; set; }
    public bool IncludeCta { get; set; }
    public bool IncludeLinks { get; set; }

    public List<InternalLinkCandidate> InternalLinks { get; set; } = [];

    public double Temperature { get; set; } = 0.5;

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
}

/// <summary>Reescribe un artículo existente manteniendo el contrato de bloques del portal.</summary>
public interface IAiArticleRewriter
{
    Task<GeneratedArticle> RewriteAsync(ArticleRewriteRequest request, CancellationToken ct = default);
}
