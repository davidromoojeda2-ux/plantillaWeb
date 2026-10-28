using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Domine.Content;
using Domine.ContentBlocks;
using Domine.Exceptions;

namespace Domine.Entities;

/// <summary>
/// Página informativa institucional (Nosotros, Privacidad, Cookies, etc.).
/// Accesible en /info/{slug} y opcionalmente listada en el pie de página.
/// </summary>
/// <remarks>
/// <see cref="BsonIgnoreExtraElementsAttribute"/> evita que fallen los documentos
/// persistidos con esquemas anteriores (por ejemplo el campo legado <c>Content</c>).
/// </remarks>
[BsonIgnoreExtraElements]
public class InformativePage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Title { get; set; } = string.Empty;

    /// <summary>Slug URL-friendly, e.g. "politica-de-privacidad".</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Cuerpo editorial estructurado y ya sanitizado.</summary>
    public List<ContentBlock> Blocks { get; set; } = [];

    /// <summary>Versión del esquema de bloques con la que se guardó el documento.</summary>
    public int SchemaVersion { get; set; } = ContentLimits.CurrentSchemaVersion;

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

        if (Blocks.Count > ContentLimits.MaxBlocks)
            throw new DomainValidationException($"La página no puede superar los {ContentLimits.MaxBlocks} bloques.", nameof(Blocks));

        if (Blocks.Count == 0)
            throw new DomainValidationException("El contenido de la página es obligatorio.", nameof(Blocks));
    }
}
