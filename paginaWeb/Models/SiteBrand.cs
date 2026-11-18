namespace paginaWeb.Models;

/// <summary>Nombre público del portal, usado en títulos, navegación y metadatos.</summary>
public static class SiteBrand
{
    public const string Name = "unautonomo";

    /// <summary>
    /// Ruta pública del logotipo usado en datos estructurados.
    /// Debe ser raster: Google no admite SVG en <c>publisher.logo</c>.
    /// </summary>
    public const string LogoPath = "/img/logos/logo.png";

    /// <summary>Ancho declarado del logotipo (px).</summary>
    public const int LogoWidth = 512;

    /// <summary>Alto declarado del logotipo (px).</summary>
    public const int LogoHeight = 512;
}
