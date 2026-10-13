using Aplication.Services;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.ViewComponents;

public class NavbarViewComponent : ViewComponent
{
    private readonly IMenuService _menuService;

    public NavbarViewComponent(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var menu = await _menuService.GetMenuAsync();
        return View(menu);
    }
}
