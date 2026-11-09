namespace paginaWeb.Areas.Admin.Models;

/// <summary>
/// Configuración del modal reutilizable de gestión de imágenes.
/// </summary>
public class ImageManagerModalViewModel
{
    public string? CurrentArticleId { get; set; }

    public string Title { get; set; } = "Biblioteca y Gestión de Imágenes";

    public string Subtitle { get; set; } =
        "Carga, renombra, edita el texto alternativo, elimina o selecciona una imagen para la portada.";

    public string ConfirmLabel { get; set; } = "Usar como Portada";

    public bool AllowSelection { get; set; } = true;
}
