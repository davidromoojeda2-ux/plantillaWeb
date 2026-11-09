namespace paginaWeb.Models;

/// <summary>Icono SVG reutilizable para las vistas Razor del panel administrativo.</summary>
public sealed record SvgIcon(string Name, string Class = "w-4 h-4", bool Filled = false);
