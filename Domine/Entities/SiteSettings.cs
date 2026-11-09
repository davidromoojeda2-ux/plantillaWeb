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

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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

    public static string ShareLabel(string key)
    {
        foreach (var share in Share)
        {
            if (string.Equals(share.Key, key, StringComparison.Ordinal))
                return share.Label;
        }

        return key;
    }
}
