namespace Aplication.DTOs;

/// <summary>Configuración global de comunidad y difusión del portal.</summary>
public class SiteSettingsDto
{
    public List<WhatsAppGroupDto> WhatsAppGroups { get; set; } = [];
    public List<SocialProfileDto> SocialProfiles { get; set; } = [];
    public List<string> EnabledShareNetworks { get; set; } = [];
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
