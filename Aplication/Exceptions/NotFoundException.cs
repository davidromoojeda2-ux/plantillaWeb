namespace Aplication.Exceptions;

/// <summary>
/// Excepción lanzada cuando un recurso o entidad solicitada no existe.
/// </summary>
public class NotFoundException : Exception
{
    public string? ResourceName { get; }
    public object? Key { get; }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} '{key}' no encontrado/a.")
    {
        ResourceName = resourceName;
        Key = key;
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
