using Aplication.DTOs;

namespace Aplication.Interfaces;

/// <summary>
/// Comprueba por HTTP que los enlaces de un artículo respondan.
/// </summary>
public interface IArticleLinkChecker
{
    Task<IReadOnlyList<ArticleLinkCheckResult>> CheckAsync(
        IEnumerable<string> links,
        Uri baseUri,
        CancellationToken ct = default);
}
