namespace Aplication.Services;

/// <summary>
/// Regla de párrafo (patrón Specification). Cada regla se evalúa por separado y
/// devuelve sus propios hallazgos, sin conocer a las demás.
/// </summary>
public interface IParagraphRule
{
    IEnumerable<ParagraphFinding> Evaluate(ParagraphContext context);
}
