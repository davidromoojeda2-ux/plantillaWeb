using Aplication.DTOs;

namespace paginaWeb.Models.Home;

/// <summary>
/// Enlaces de navegación entre las páginas institucionales.
/// Se usa tanto en el panel de escritorio como en el desplegable móvil.
/// </summary>
public sealed record InstitutionalNavViewModel(
    IReadOnlyList<PageResponseDto> Pages,
    string CurrentSlug,
    bool IsMobile);

/// <summary>Encabezado reutilizable de los menús laterales plegables.</summary>
public sealed record CollapsibleMenuHeaderViewModel(string Title);

/// <summary>
/// Contenido compartido de las tarjetas de "Últimas publicaciones" y "Top Más Leídos":
/// categoría, título, fecha y, opcionalmente, el contador de lecturas y la flecha.
/// </summary>
public sealed record EntryCardContentViewModel(
    PostCardViewModel Entry,
    string FallbackMeta,
    bool ShowViews);
