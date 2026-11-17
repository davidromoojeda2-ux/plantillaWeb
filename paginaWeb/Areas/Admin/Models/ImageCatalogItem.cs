namespace paginaWeb.Areas.Admin.Models;

/// <summary>
/// Elemento del catálogo JSON de imágenes. Lo consumen el gestor de portadas
/// y el selector de medios del editor de bloques.
/// </summary>
public sealed record ImageCatalogItem(
    string Id,
    string FileName,
    string Alt,
    int Width,
    int Height,
    string UploadedAt,
    string? Url,
    string? ThumbnailUrl,
    string Storage,
    bool IsUsed,
    string? UsedInArticleId,
    string? UsedInArticleTitle,
    bool IsCurrentArticle);
