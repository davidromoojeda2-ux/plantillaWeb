namespace Domine.Exceptions;

/// <summary>
/// Excepción base para errores de reglas de negocio e invariantes del dominio.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
