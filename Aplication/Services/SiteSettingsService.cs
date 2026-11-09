using Aplication.DTOs;
using Aplication.Interfaces;
using Domine.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Aplication.Services;

public interface ISiteSettingsService
{
    Task<SiteSettingsDto> GetAsync();
    Task SaveWhatsAppGroupsAsync(IEnumerable<WhatsAppGroupDto>? groups);
    Task SaveSocialProfilesAsync(IEnumerable<SocialProfileDto>? profiles);
    Task SaveShareNetworksAsync(IEnumerable<string>? networks);
}

/// <summary>
/// Administra la configuración global de comunidad y difusión. Mantiene una copia en caché
/// para no consultar MongoDB en cada render del navbar, footer y barra de compartir.
/// </summary>
public class SiteSettingsService : ISiteSettingsService
{
    private const string CacheKey = "site-settings";
    private const int MaxWhatsAppGroups = 50;
    private const int MaxSocialProfiles = 10;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    private readonly ISiteSettingsRepository _repository;
    private readonly IMemoryCache _cache;

    public SiteSettingsService(ISiteSettingsRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<SiteSettingsDto> GetAsync()
    {
        if (_cache.TryGetValue(CacheKey, out SiteSettingsDto? cached) && cached is not null)
            return cached;

        var dto = ToDto(await _repository.GetAsync());
        _cache.Set(CacheKey, dto, CacheDuration);
        return dto;
    }

    public async Task SaveWhatsAppGroupsAsync(IEnumerable<WhatsAppGroupDto>? groups)
    {
        var settings = await _repository.GetAsync();
        settings.WhatsAppGroups = NormalizeWhatsAppGroups(groups);
        await _repository.SaveAsync(settings);
        _cache.Remove(CacheKey);
    }

    public async Task SaveSocialProfilesAsync(IEnumerable<SocialProfileDto>? profiles)
    {
        var settings = await _repository.GetAsync();
        settings.SocialProfiles = NormalizeSocialProfiles(profiles);
        await _repository.SaveAsync(settings);
        _cache.Remove(CacheKey);
    }

    public async Task SaveShareNetworksAsync(IEnumerable<string>? networks)
    {
        var requested = (networks ?? [])
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim().ToLowerInvariant())
            .Where(SocialNetworks.IsShare)
            .ToHashSet(StringComparer.Ordinal);

        var settings = await _repository.GetAsync();
        settings.EnabledShareNetworks = SocialNetworks.Share
            .Select(s => s.Key)
            .Where(requested.Contains)
            .ToList();

        await _repository.SaveAsync(settings);
        _cache.Remove(CacheKey);
    }

    private static List<WhatsAppGroupLink> NormalizeWhatsAppGroups(IEnumerable<WhatsAppGroupDto>? groups)
    {
        var result = new List<WhatsAppGroupLink>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups ?? [])
        {
            var name = (group.Name ?? string.Empty).Trim();
            var url = (group.Url ?? string.Empty).Trim();

            if (name.Length == 0 && url.Length == 0)
                continue;

            if (name.Length == 0)
                throw new InvalidOperationException("Cada grupo de WhatsApp debe tener un nombre.");

            if (!IsHttpUrl(url))
                throw new InvalidOperationException($"El enlace del grupo \"{name}\" no es una URL http/https válida.");

            if (!seenUrls.Add(url))
                continue;

            if (result.Count >= MaxWhatsAppGroups)
                throw new InvalidOperationException($"Solo se permiten hasta {MaxWhatsAppGroups} grupos de WhatsApp.");

            var link = new WhatsAppGroupLink
            {
                Name = Truncate(name, 80),
                Url = Truncate(url, 500),
                Order = result.Count,
                Active = group.Active
            };

            if (!string.IsNullOrWhiteSpace(group.Id))
                link.Id = group.Id.Trim();

            result.Add(link);
        }

        return result;
    }

    private static List<SocialProfileLink> NormalizeSocialProfiles(IEnumerable<SocialProfileDto>? profiles)
    {
        var result = new List<SocialProfileLink>();
        var seenPlatforms = new HashSet<string>(StringComparer.Ordinal);

        foreach (var profile in profiles ?? [])
        {
            var platform = (profile.Platform ?? string.Empty).Trim().ToLowerInvariant();
            var url = (profile.Url ?? string.Empty).Trim();

            if (url.Length == 0)
                continue;

            if (!SocialNetworks.IsProfile(platform))
                throw new InvalidOperationException($"La red social \"{profile.Platform}\" no está soportada.");

            if (!IsHttpUrl(url))
                throw new InvalidOperationException($"El enlace de {SocialNetworks.ProfileLabel(platform)} no es una URL http/https válida.");

            if (!seenPlatforms.Add(platform))
                throw new InvalidOperationException($"La red {SocialNetworks.ProfileLabel(platform)} está repetida.");

            if (result.Count >= MaxSocialProfiles)
                throw new InvalidOperationException($"Solo se permiten hasta {MaxSocialProfiles} redes sociales.");

            result.Add(new SocialProfileLink
            {
                Platform = platform,
                Url = Truncate(url, 500),
                Order = result.Count,
                Active = profile.Active
            });
        }

        return result;
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static SiteSettingsDto ToDto(SiteSettings settings) => new()
    {
        WhatsAppGroups = settings.WhatsAppGroups
            .OrderBy(g => g.Order)
            .Select(g => new WhatsAppGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                Url = g.Url,
                Order = g.Order,
                Active = g.Active
            })
            .ToList(),
        SocialProfiles = settings.SocialProfiles
            .OrderBy(p => p.Order)
            .Select(p => new SocialProfileDto
            {
                Platform = p.Platform,
                Url = p.Url,
                Order = p.Order,
                Active = p.Active
            })
            .ToList(),
        EnabledShareNetworks = settings.EnabledShareNetworks
            .Where(SocialNetworks.IsShare)
            .Distinct(StringComparer.Ordinal)
            .ToList()
    };
}
