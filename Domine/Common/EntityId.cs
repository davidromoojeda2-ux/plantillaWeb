namespace Domine.Common;

/// <summary>
/// Fuente única del formato de identificadores del dominio.
/// Aísla a <c>Domine</c> y <c>Aplication</c> de cualquier detalle del motor de
/// persistencia: aquí se define cómo se generan y validan los identificadores.
/// Al migrar de motor, este es el único punto que debe cambiar.
/// </summary>
public static class EntityId
{
    /// <summary>
    /// Genera un identificador de 24 caracteres hexadecimales, compatible con el
    /// formato que usa actualmente la persistencia. Si en el futuro se
    /// migra a otro motor (por ejemplo SQL), aquí se cambia a
    /// <c>Guid.NewGuid().ToString("N")</c>.
    /// </summary>
    public static string New() => Guid.NewGuid().ToString("N")[..24];

    /// <summary>
    /// Indica si el valor tiene el formato de identificador aceptado por el dominio.
    /// Sustituye a las validaciones heredadas del motor de persistencia anterior.
    /// </summary>
    public static bool IsValid(string? id) =>
        id is { Length: 24 } && id.All(char.IsAsciiHexDigit);
}
