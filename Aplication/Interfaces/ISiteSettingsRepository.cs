using Domine.Entities;

namespace Aplication.Interfaces;

/// <summary>Persistencia del documento único de configuración global del sitio.</summary>
public interface ISiteSettingsRepository
{
    Task<SiteSettings> GetAsync();
    Task SaveAsync(SiteSettings settings);
}
