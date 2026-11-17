using Aplication.DTOs;
using Aplication.Mappers;
using Aplication.Services;
using Infrastructure.Storage;
using paginaWeb.Areas.Admin.Models;

namespace paginaWeb.Helpers;

/// <summary>
/// Catálogo de imágenes junto con el artículo que usa cada una. Lo comparten el
/// listado del gestor de imágenes y el selector de medios del editor de bloques.
/// </summary>
public static class ImageCatalogHelper
{
    /// <summary>Obtiene las imágenes y las proyecta al contrato JSON del catálogo.</summary>
    public static async Task<List<ImageCatalogItem>> BuildAsync(
        ImageService imageService,
        IArticleService articleService,
        string? currentArticleId,
        int imageTake = 500,
        int articleTake = 500)
    {
        var images = await imageService.GetAllAsync(0, imageTake);
        var articles = await articleService.GetAllAdminAsync(1, articleTake);

        ApplyArticleUsage(images, articles);

        return images.Select(img => new ImageCatalogItem(
                img.Id,
                img.OriginalFileName,
                img.Alt,
                img.OriginalWidth,
                img.OriginalHeight,
                img.UploadedAt.ToString("dd/MM/yyyy"),
                ImageMapper.GetBestUrl(img) ?? img.Variants.FirstOrDefault()?.PublicUrl,
                ImageMapper.GetThumbnailUrl(img) ?? ImageMapper.GetBestUrl(img),
                img.Storage,
                !string.IsNullOrEmpty(img.ArticleId),
                img.ArticleId,
                img.ArticleTitle,
                !string.IsNullOrEmpty(currentArticleId) && img.ArticleId == currentArticleId))
            .ToList();
    }

    /// <summary>
    /// Completa cada imagen con el artículo que la usa, ya sea como portada o en su galería.
    /// </summary>
    public static void ApplyArticleUsage(
        IReadOnlyList<ImageMetadataResponseDto> images,
        IReadOnlyList<ArticleResponseDto> articles)
    {
        var usageMap = new Dictionary<string, (string ArticleId, string ArticleTitle)>();
        var articlesDict = new Dictionary<string, string>();

        foreach (var article in articles)
        {
            articlesDict[article.Id] = article.Title;

            if (!string.IsNullOrEmpty(article.CoverImageId))
            {
                usageMap.TryAdd(article.CoverImageId, (article.Id, article.Title));
            }

            if (article.ImageIds is null) continue;

            foreach (var imageId in article.ImageIds)
            {
                if (!string.IsNullOrEmpty(imageId))
                {
                    usageMap.TryAdd(imageId, (article.Id, article.Title));
                }
            }
        }

        foreach (var image in images)
        {
            if (usageMap.TryGetValue(image.Id, out var usage))
            {
                image.ArticleId = usage.ArticleId;
                image.ArticleTitle = usage.ArticleTitle;
            }
            else if (!string.IsNullOrEmpty(image.ArticleId) &&
                     articlesDict.TryGetValue(image.ArticleId, out var title))
            {
                image.ArticleTitle = title;
            }
        }
    }
}
