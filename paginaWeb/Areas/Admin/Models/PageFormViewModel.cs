using System.ComponentModel.DataAnnotations;
using Aplication.DTOs;
using Aplication.DTOs.ContentBlocks;
using Aplication.Serialization;

namespace paginaWeb.Areas.Admin.Models;

/// <summary>
/// ViewModel de creación/edición de páginas informativas con el cuerpo
/// serializado por el editor de bloques.
/// </summary>
public class PageFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "El cuerpo de la página es obligatorio.")]
    public string BlocksJson { get; set; } = "[]";

    [StringLength(300)]
    public string Subtitle { get; set; } = string.Empty;

    public int Order { get; set; } = 1;
    public bool ShowInFooter { get; set; } = true;

    public PageDto ToDto(IReadOnlyList<ContentBlockDto> blocks) => new()
    {
        Title = Title,
        Slug = Slug,
        Blocks = [.. blocks],
        Subtitle = Subtitle,
        Order = Order,
        ShowInFooter = ShowInFooter
    };

    public static PageFormViewModel FromDto(PageResponseDto page) => new()
    {
        Id = page.Id,
        Title = page.Title,
        Slug = page.Slug,
        BlocksJson = ContentBlockJson.Serialize(page.Blocks),
        Subtitle = page.Subtitle,
        Order = page.Order,
        ShowInFooter = page.ShowInFooter
    };
}
