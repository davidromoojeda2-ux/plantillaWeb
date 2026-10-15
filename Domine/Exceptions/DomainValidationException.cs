namespace Domine.Exceptions;

/// <summary>
/// Excepción lanzada cuando una invariante o regla de validación de una entidad de dominio es violada.
/// Permite especificar la propiedad afectada para mapeo preciso en la capa de presentación.
/// </summary>
public class DomainValidationException : DomainException
{
    public string? PropertyName { get; }

    public DomainValidationException(string message, string? propertyName = null)
        : base(message)
    {
        PropertyName = propertyName;
    }

    public DomainValidationException(string message, string? propertyName, Exception innerException)
        : base(message, innerException)
    {
        PropertyName = propertyName;
    }
}
