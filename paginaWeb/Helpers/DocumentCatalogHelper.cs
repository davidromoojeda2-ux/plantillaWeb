using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Mappers;
using Aplication.Services;
using paginaWeb.Areas.Admin.Models;

namespace paginaWeb.Helpers;

/// <summary>
/// Proyecta documentos y sus artículos asociados al contrato JSON que consumen
/// la biblioteca administrativa y el modal de selección del editor.
/// </summary>
public static class DocumentCatalogHelper
{
    public static async Task<List<DocumentCatalogItem>> BuildAsync(
        IDocumentService documentService,
        IArticleService articleService,
        string? currentArticleId,
        int documentTake = 500,
        int articleTake = 500)
    {
        var documents = await documentService.GetAllAsync(0, documentTake);
        var articles = await articleService.GetAllAdminAsync(1, articleTake);

        ApplyArticleUsage(documents, articles);

        return documents.Select(document => new DocumentCatalogItem(
                document.Id,
                document.DisplayName,
                document.OriginalFileName,
                document.Description,
                document.Extension,
                document.ContentType,
                document.SizeBytes,
                DocumentMapper.FormatSize(document.SizeBytes),
                document.Storage,
                document.IsPublic,
                document.DownloadCount,
                document.UploadedAt.ToString("dd/MM/yyyy"),
                $"/admin/documentos/descargar/{document.Id}",
                !string.IsNullOrEmpty(document.ArticleId),
                document.ArticleId,
                document.ArticleTitle,
                !string.IsNullOrEmpty(currentArticleId) && document.ArticleId == currentArticleId))
            .ToList();
    }

    public static void ApplyArticleUsage(
        IReadOnlyList<DocumentResponseDto> documents,
        IReadOnlyList<ArticleResponseDto> articles)
    {
        var usage = new Dictionary<string, (string ArticleId, string ArticleTitle)>();
        var articlesById = articles.ToDictionary(article => article.Id, article => article.Title);

        foreach (var article in articles)
        {
            foreach (var documentId in article.DocumentIds ?? [])
            {
                if (!string.IsNullOrWhiteSpace(documentId))
                    usage.TryAdd(documentId, (article.Id, article.Title));
            }
        }

        foreach (var document in documents)
        {
            if (usage.TryGetValue(document.Id, out var reference))
            {
                document.ArticleId = reference.ArticleId;
                document.ArticleTitle = reference.ArticleTitle;
            }
            else if (!string.IsNullOrEmpty(document.ArticleId) &&
                     articlesById.TryGetValue(document.ArticleId, out var title))
            {
                document.ArticleTitle = title;
            }
        }
    }
}
