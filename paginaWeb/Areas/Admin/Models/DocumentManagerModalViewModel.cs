namespace paginaWeb.Areas.Admin.Models;

/// <summary>Configuración del modal reutilizable de selección de documentos.</summary>
public class DocumentManagerModalViewModel
{
    public string? CurrentArticleId { get; set; }
    public List<string> SelectedIds { get; set; } = [];
    public string Title { get; set; } = "Documentos adjuntos";
    public string Subtitle { get; set; } = "Selecciona los documentos que se mostrarán en la sección de descargas del artículo.";
    public List<string> AllowedExtensions { get; set; } = [];
}
