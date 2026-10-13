using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.Entities;

/// <summary>
/// Categoría del portal de noticias con soporte de jerarquía (padre/hijo).
/// Permite hasta dos niveles: categoría raíz (ParentId == null) y subcategoría.
/// </summary>
public class Category
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>Id de la categoría padre; null indica categoría raíz.</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Slug único para URL amigable, e.g. "tecnologia".</summary>
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Orden de aparición en el menú (ascendente).</summary>
    public int Order { get; set; } = 0;

    /// <summary>Si es true, aparece en el menú de navegación.</summary>
    public bool ShowInMenu { get; set; } = true;

    /// <summary>Si es false, la categoría está oculta en el sitio público.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
