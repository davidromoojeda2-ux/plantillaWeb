namespace paginaWeb.Models;

/// <summary>Icono SVG reutilizable para las vistas Razor.</summary>
public sealed record SvgIcon(
    string Name,
    string Class = "w-4 h-4",
    bool Filled = false,
    string StrokeWidth = "1.8",
    string? Width = null,
    string? Height = null);
