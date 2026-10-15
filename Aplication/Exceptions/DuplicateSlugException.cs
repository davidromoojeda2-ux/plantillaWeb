namespace Aplication.Exceptions;

/// <summary>
/// Excepción específica para conflictos generados por duplicidad de slugs en la persistencia.
/// </summary>
public class DuplicateSlugException : ConflictException
{
    public string Slug { get; }

    public DuplicateSlugException(string slug, string? message = null)
        : base(message ?? $"El slug '{slug}' ya está en uso.", "Slug")
    {
        Slug = slug;
    }
}
