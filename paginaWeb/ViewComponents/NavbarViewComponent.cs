using Aplication.Services;
using Microsoft.AspNetCore.Mvc;
using paginaWeb.Models;

namespace paginaWeb.ViewComponents;

public class NavbarViewComponent : ViewComponent
{
    private readonly IMenuService _menuService;
    private readonly ISiteSettingsService _siteSettingsService;

    public NavbarViewComponent(IMenuService menuService, ISiteSettingsService siteSettingsService)
    {
        _menuService = menuService;
        _siteSettingsService = siteSettingsService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string viewName = "Default")
    {
        var menu = await _menuService.GetMenuAsync();
        var settings = await _siteSettingsService.GetAsync();

        var model = new NavbarViewModel
        {
            Menu = menu ?? [],
            WhatsAppGroups = settings.WhatsAppGroups
                .Where(g => g.Active && !string.IsNullOrWhiteSpace(g.Url))
                .OrderBy(g => g.Order)
                .ToList()
        };

        return View(viewName, model);
    }
}
