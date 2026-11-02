namespace Aplication.DTOs.Automation;

/// <summary>Estándar de generación derivado de la longitud del artículo.</summary>
public sealed record ArticleGenerationStandard(
    int MinBlocks,
    int MaxBlocks,
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

        // Aproximadamente un bloque por cada 180 palabras, con un mínimo y un margen.
        var minBlocks = Math.Clamp((int)Math.Round(target / 180.0) + 2, 4, 20);
        var maxBlocks = Math.Clamp(minBlocks + 5, minBlocks + 2, 30);

        // Margen de tokens suficiente para el contenido y el formato JSON.
        var maxTokens = Math.Max(1024, (int)Math.Ceiling(target * 3.5) + 2000);

        var tolerance = target >= 2000 ? 30 : target >= 1000 ? 25 : 20;

        return new ArticleGenerationStandard(minBlocks, maxBlocks, maxTokens, tolerance);
    }
}
