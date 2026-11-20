using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>
/// Reglas estructurales compartidas por la validación del editor y la generación.
/// Evita que ambas rutas mantengan copias separadas de los mismos criterios.
/// </summary>
public static class ContentBlockRules
{
    /// <summary>Indica si un bloque está vacío o no tiene la estructura mínima.</summary>
    public static bool IsEmpty(ContentBlockDto block) => block switch
    {
        ContentParagraphBlockDto p => string.IsNullOrWhiteSpace(p.Text),
        ContentHeadingBlockDto h => string.IsNullOrWhiteSpace(h.Text),
        ContentQuoteBlockDto q => string.IsNullOrWhiteSpace(q.Text),
        ContentCalloutBlockDto c => string.IsNullOrWhiteSpace(c.Text),
        ContentBulletListBlockDto l => l.Items.Count < 2 || l.Items.All(string.IsNullOrWhiteSpace),
        ContentOrderedListBlockDto l => l.Items.Count < 2 || l.Items.All(string.IsNullOrWhiteSpace),
        ContentTableBlockDto t => t.Headers.Count == 0 || t.Rows.Count < 2,
        ContentCodeBlockDto c => string.IsNullOrWhiteSpace(c.Code),
        _ => false
    };
}
