namespace Aplication.DTOs;

/// <summary>
/// Señal mínima de un artículo publicado para calcular el ranking de secciones
/// del pie de página (vistas y volumen de contenido por categoría).
/// </summary>
public class CategoryArticleSignalDto
{
    public string? CategoryId { get; set; }
    public string? SubcategoryId { get; set; }
    public long ViewCount { get; set; }
}
