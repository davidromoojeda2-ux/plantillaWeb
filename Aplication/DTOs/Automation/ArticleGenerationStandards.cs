namespace Aplication.DTOs.Automation;

/// <summary>Estándar de generación derivado de la longitud del artículo.</summary>
public sealed record ArticleGenerationStandard(
    int MinBlocks,
    int MaxTokens,
    int WordTolerancePercent);

/// <summary>
/// Calcula los valores recomendados de bloques, tokens y tolerancia para cada longitud.
/// Se usa para preconfigurar la generación automáticamente al elegir las palabras.
/// </summary>
public static class ArticleGenerationStandards
{
    public static ArticleGenerationStandard ForWords(int words)
    {
        var target = Math.Clamp(words, 100, 5000);

        // Solo se exige un mínimo de bloques; no se impone un máximo (era innecesario y
        // rechazaba artículos ricos con FAQ/CTA/listas bien estructurados).
        var minBlocks = Math.Clamp((int)Math.Floor(target / 200.0) + 3, 4, 30);

        // Margen de tokens suficiente para el contenido y el formato JSON (sin tope fijo:
        // cada proveedor aplica su propio límite de salida).
        var maxTokens = Math.Max(1024, (int)Math.Ceiling(target * 3.5) + 2000);

        // La IA varía la extensión; un 30 % de margen evita rechazos por diferencias menores.
        var tolerance = target >= 1000 ? 30 : 25;

        return new ArticleGenerationStandard(minBlocks, maxTokens, tolerance);
    }
}
