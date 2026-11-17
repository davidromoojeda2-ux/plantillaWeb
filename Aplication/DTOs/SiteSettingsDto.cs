namespace Aplication.DTOs;

/// <summary>Configuración global de comunidad y difusión del portal.</summary>
public class SiteSettingsDto
{
    public List<WhatsAppGroupDto> WhatsAppGroups { get; set; } = [];
    public List<SocialProfileDto> SocialProfiles { get; set; } = [];
    public List<string> EnabledShareNetworks { get; set; } = [];
    public ImageQualitySettingsDto ImageQuality { get; set; } = new();
}

/// <summary>Calidad de compresión WebP configurada para las nuevas imágenes.</summary>
public class ImageQualitySettingsDto
{
    public const int DefaultQuality = Domine.Entities.ImageQualitySettings.DefaultQuality;
    public const int MinQuality = Domine.Entities.ImageQualitySettings.MinQuality;
    public const int MaxQuality = Domine.Entities.ImageQualitySettings.MaxQuality;

    public int Quality { get; set; } = DefaultQuality;

    /// <summary>Calidad para 400 px; null usa la global.</summary>
    public int? QualitySmall { get; set; }

    /// <summary>Calidad para 800 px; null usa la global.</summary>
    public int? QualityMedium { get; set; }

    /// <summary>Calidad para 1600 px; null usa la global.</summary>
    public int? QualityLarge { get; set; }

    public int ForWidth(int width) => width switch
    {
        <= 400 => QualitySmall ?? Quality,
        <= 800 => QualityMedium ?? Quality,
        _ => QualityLarge ?? Quality
    };
}

public class WhatsAppGroupDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool Active { get; set; } = true;
}

public class SocialProfileDto
{
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool Active { get; set; } = true;
}
