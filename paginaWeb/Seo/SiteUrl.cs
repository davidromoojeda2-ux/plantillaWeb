using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace paginaWeb.Seo;

/// <summary>Resuelve la URL base pública del sitio para enlaces absolutos.</summary>
public static class SiteUrl
{
    public static string Resolve(HttpContext context, IConfiguration configuration)
    {
        var configured = configuration["Site:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');

        var request = context.Request;
        return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
    }
}
