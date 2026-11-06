using Aplication.DTOs;
using Aplication.Services;

namespace paginaWeb.Models;

/// <summary>Datos del navbar público: menú de categorías y grupos de WhatsApp activos.</summary>
public class NavbarViewModel
{
    public List<MenuCategory> Menu { get; set; } = [];
    public List<WhatsAppGroupDto> WhatsAppGroups { get; set; } = [];
}
