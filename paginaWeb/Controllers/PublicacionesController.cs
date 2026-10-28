using Aplication.DTOs;
using Aplication.Mappers;
using Aplication.Services;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;
using paginaWeb.Models.Category;
using paginaWeb.Models.Publicaciones;

namespace paginaWeb.Controllers;

[Route("publicaciones")]
public class PublicacionesController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ImageService _imageService;

    public PublicacionesController(
        IArticleService articleService,
        ICategoryService categoryService,
        ImageService imageService)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _imageService = imageService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var query = q?.Trim() ?? string.Empty;
        var (items, total) = string.IsNullOrWhiteSpace(query)
            ? await _articleService.GetPublishedAsync(page, 9)
            : await _articleService.SearchAsync(query, page, 9);

        var categories = (await _categoryService.GetAllAsync())
            .ToDictionary(c => c.Id, c => c.Name);

        var images = await _imageService.GetVariantsDictAsync(
            items.Select(a => a.CoverImageId).Where(id => !string.IsNullOrEmpty(id))!);

        var cardItems = items.Select(art =>
        {
            var artCover = art.CoverImageId != null && images.TryGetValue(art.CoverImageId, out var img) ? img : null;
            var artImgUrl = ImageMapper.GetCardUrl(art, artCover);
            var artCat = art.CategoryId != null && categories.TryGetValue(art.CategoryId, out var catName)
                ? catName
                : "General";

            return new ArticleCardViewModel(art, artImgUrl, artCat);
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)total / 9);

        var viewModel = new PublicacionesPageViewModel(
            Query: query,
            CurrentPage: page,
            TotalPages: totalPages,
            TotalCount: total,
            Items: cardItems);

        return View(viewModel);
    }
}
