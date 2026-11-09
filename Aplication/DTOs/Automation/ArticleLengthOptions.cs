namespace Aplication.DTOs.Automation;

public static class ArticleLengthOptions
{
    /// <summary>Cantidades de palabras sugeridas para el desplegable.</summary>
    public static readonly IReadOnlyList<int> Words = [500, 800, 1000, 1500, 2000, 2500, 3000, 3500];
    public static int EstimateMaxTokens(int words)
    {
        var w = Math.Clamp(words, 100, 5000);
        return Math.Max(512, w * 3 + 2000);
    }
}
