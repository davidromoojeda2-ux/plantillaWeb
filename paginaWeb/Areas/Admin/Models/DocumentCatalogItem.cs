namespace paginaWeb.Areas.Admin.Models;

/// <summary>Elemento JSON del catálogo de documentos usado por el CMS.</summary>
public sealed record DocumentCatalogItem(
    string Id,
    string DisplayName,
    string OriginalFileName,
    string? Description,
    string Extension,
    string ContentType,
    long SizeBytes,
    string SizeLabel,
    string Storage,
    bool IsPublic,
    long DownloadCount,
    string UploadedAt,
    string DownloadUrl,
    bool IsUsed,
    string? UsedInArticleId,
    string? UsedInArticleTitle,
    bool IsCurrentArticle);
