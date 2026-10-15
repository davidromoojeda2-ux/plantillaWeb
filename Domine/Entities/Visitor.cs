using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.Entities;

/// <summary>
/// Visitante anónimo que interactúa con el portal (comentarios).
/// No almacena contraseñas; se identifica por correo con hash de IP para privacidad.
/// </summary>
public class Visitor
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = string.Empty;

    /// <summary>Correo del visitante (no se publica, solo para referencia interna).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash SHA-256 de la IP para control de tasa sin exponer la IP real.</summary>
    public string IpHash { get; set; } = string.Empty;

    /// <summary>Indica si el visitante aceptó el aviso de cookies.</summary>
    public bool ConsentCookies { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
