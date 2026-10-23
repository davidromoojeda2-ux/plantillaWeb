using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Domine.Exceptions;

namespace Domine.Entities;

/// <summary>
/// Página informativa institucional (Nosotros, Privacidad, Cookies, etc.).
/// Accesible en /info/{slug} y opcionalmente listada en el pie de página.
/// </summary>
public class InformativePage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Title { get; set; } = string.Empty;

    /// <summary>Slug URL-friendly, e.g. "politica-de-privacidad".</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Contenido HTML sanitizado de la sección.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Subtítulo descriptivo que aparece en el hero de la página pública.</summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>Orden de aparición en el footer (ascendente).</summary>
    public int Order { get; set; } = 0;

    /// <summary>Si es true, aparece como enlace en el pie de página.</summary>
    public bool ShowInFooter { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Valida las invariantes de la página informativa según las reglas del dominio.
    /// Lanza <see cref="DomainValidationException"/> si el estado es inconsistente.
    /// </summary>
    public void ValidarInvariantes()
    {
        if (string.IsNullOrWhiteSpace(Title))
            throw new DomainValidationException("El título de la página es obligatorio.", nameof(Title));

        if (string.IsNullOrWhiteSpace(Slug))
            throw new DomainValidationException("El slug de la página es obligatorio.", nameof(Slug));

        if (string.IsNullOrWhiteSpace(Content))
            throw new DomainValidationException("El contenido de la página es obligatorio.", nameof(Content));
    }
}

