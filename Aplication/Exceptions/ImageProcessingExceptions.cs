namespace Aplication.Exceptions;

/// <summary>El archivo no es un formato de imagen permitido por el portal.</summary>
public sealed class UnsupportedImageFormatException : Exception
{
    public UnsupportedImageFormatException(string message) : base(message)
    {
    }
}

/// <summary>El archivo tiene un formato reconocido pero su contenido está dañado.</summary>
public sealed class InvalidImageContentException : Exception
{
    public InvalidImageContentException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
