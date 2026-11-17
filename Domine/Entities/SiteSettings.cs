using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.Entities;

/// <summary>
/// Configuración global del portal para comunidad y difusión. Se persiste como documento único.
/// </summary>
[BsonIgnoreExtraElements]
public class SiteSettings
{
    /// <summary>Identificador fijo que garantiza un único documento de configuración.</summary>
    public const string SingletonId = "site-settings";

    [BsonId]
    public string Id { get; set; } = SingletonId;

    /// <summary>Grupos de WhatsApp que se muestran en el modal público del botón "Únete".</summary>
    public List<WhatsAppGroupLink> WhatsAppGroups { get; set; } = [];

    /// <summary>Perfiles de redes sociales que se muestran en el footer del sitio.</summary>
    public List<SocialProfileLink> SocialProfiles { get; set; } = [];

    /// <summary>Redes habilitadas en la barra de compartir de los artículos.</summary>
    public List<string> EnabledShareNetworks { get; set; } =
    [
        SocialNetworks.X,
        SocialNetworks.Facebook,
        SocialNetworks.LinkedIn,
        SocialNetworks.WhatsApp
    ];

    /// <summary>Calidad de compresión WebP aplicada a las nuevas imágenes.</summary>
    public ImageQualitySettings ImageQuality { get; set; } = new();

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Calidad de codificación WebP para las variantes generadas al subir una imagen.
/// Un valor nulo en un tamaño concreto indica que se usa la calidad global.
/// </summary>
[BsonIgnoreExtraElements]
public class ImageQualitySettings
{
    public const int DefaultQuality = 80;
    public const int MinQuality = 1;
    public const int MaxQuality = 100;

    public int Quality { get; set; } = DefaultQuality;

    /// <summary>Calidad específica para la variante de 400 px (miniatura).</summary>
    public int? QualitySmall { get; set; }

    /// <summary>Calidad específica para la variante de 800 px.</summary>
    public int? QualityMedium { get; set; }

    /// <summary>Calidad específica para la variante de 1600 px.</summary>
    public int? QualityLarge { get; set; }

    /// <summary>Resuelve la calidad efectiva para un ancho de variante.</summary>
    public int ForWidth(int width) => width switch
    {
        <= 400 => QualitySmall ?? Quality,
        <= 800 => QualityMedium ?? Quality,
        _ => QualityLarge ?? Quality
    };
}

[BsonIgnoreExtraElements]
public class WhatsAppGroupLink
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool Active { get; set; } = true;
}

[BsonIgnoreExtraElements]
public class SocialProfileLink
{
    /// <summary>Clave de la red social (ver <see cref="SocialNetworks"/>).</summary>
    public string Platform { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool Active { get; set; } = true;
}

/// <summary>Catálogo de redes sociales soportadas por el portal.</summary>
public static class SocialNetworks
{
    public const string X = "x";
    public const string Facebook = "facebook";
    public const string Instagram = "instagram";
    public const string YouTube = "youtube";
    public const string TikTok = "tiktok";
    public const string LinkedIn = "linkedin";
    public const string WhatsApp = "whatsapp";

    /// <summary>Redes disponibles en el footer del sitio.</summary>
    public static readonly (string Key, string Label)[] Profiles =
    [
        (X, "X (Twitter)"),
        (Facebook, "Facebook"),
        (Instagram, "Instagram"),
        (YouTube, "YouTube"),
        (TikTok, "TikTok"),
        (LinkedIn, "LinkedIn"),
        (WhatsApp, "WhatsApp")
    ];

    /// <summary>Redes disponibles en la barra de compartir artículos.</summary>
    public static readonly (string Key, string Label)[] Share =
    [
        (X, "X / Twitter"),
        (Facebook, "Facebook"),
        (LinkedIn, "LinkedIn"),
        (WhatsApp, "WhatsApp")
    ];

    public static bool IsProfile(string key) =>
        Profiles.Any(p => string.Equals(p.Key, key, StringComparison.Ordinal));

    public static bool IsShare(string key) =>
        Share.Any(s => string.Equals(s.Key, key, StringComparison.Ordinal));

    public static string ProfileLabel(string key)
    {
        foreach (var profile in Profiles)
        {
            if (string.Equals(profile.Key, key, StringComparison.Ordinal))
                return profile.Label;
        }

        return key;
    }
}
