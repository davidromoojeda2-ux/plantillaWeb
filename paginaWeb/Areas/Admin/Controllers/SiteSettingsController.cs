using Aplication.DTOs;
using Aplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace paginaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/ajustes")]
public class SiteSettingsController : Controller
{
    private readonly ISiteSettingsService _settingsService;

    public SiteSettingsController(ISiteSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpPost("whatsapp-groups")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWhatsAppGroups([FromBody] List<WhatsAppGroupDto>? groups)
    {
        try
        {
            await _settingsService.SaveWhatsAppGroupsAsync(groups);
            return Json(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("social-profiles")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSocialProfiles([FromBody] List<SocialProfileDto>? profiles)
    {
        try
        {
            await _settingsService.SaveSocialProfilesAsync(profiles);
            return Json(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("share-networks")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveShareNetworks([FromBody] List<string>? networks)
    {
        try
        {
            await _settingsService.SaveShareNetworksAsync(networks);
            return Json(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
