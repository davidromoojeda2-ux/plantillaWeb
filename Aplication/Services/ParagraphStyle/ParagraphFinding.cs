namespace Aplication.Services;

/// <summary>Gravedad de un hallazgo de párrafo.</summary>
public enum ParagraphSeverity
{
    /// <summary>Se informa y se sugiere corregir, pero no bloquea ni fuerza reintento.</summary>
    Soft,

    /// <summary>Defecto evidente: fuerza un reintento correctivo en la automatización.</summary>
    Hard
}

/// <summary>Hallazgo producido por una regla de párrafo.</summary>
public sealed record ParagraphFinding(
    string Code,
    ParagraphSeverity Severity,
    string Message,
    int BlockIndex,
    int Value = 0);
