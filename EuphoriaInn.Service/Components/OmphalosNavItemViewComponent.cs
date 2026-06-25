using EuphoriaInn.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EuphoriaInn.Service.Components;

public class OmphalosNavItemViewComponent(IAdminSettingService adminSettingService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await adminSettingService.GetSettingsAsync();
        if (!settings.IsConfigured)
            return Content(string.Empty);   // renders nothing — no HTML emitted when unconfigured
        return View(settings);              // passes IntegrationSettings to Default.cshtml
    }
}
