namespace Aplication.Exceptions;

/// <summary>
/// Excepción lanzada cuando ocurre un conflicto de estado o violación de unicidad en la capa de aplicación.
/// </summary>
public class ConflictException : Exception
{
    public string? PropertyName { get; }

    public ConflictException(string message, string? propertyName = null) : base(message)
    {
        PropertyName = propertyName;
    }

    public ConflictException(string message, string? propertyName, Exception innerException) : base(message, innerException)
    {
        PropertyName = propertyName;
    }
}
