using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

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

    /// <summary>Orden de aparición en el footer (ascendente).</summary>
    public int Order { get; set; } = 0;

    /// <summary>Si es true, aparece como enlace en el pie de página.</summary>
    public bool ShowInFooter { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
