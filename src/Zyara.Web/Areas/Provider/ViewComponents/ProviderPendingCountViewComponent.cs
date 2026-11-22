using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Web.Areas.Provider.ViewComponents;

public class ProviderPendingCountViewComponent : ViewComponent
{
    private readonly IProviderDashboardService _dashboardService;
    private readonly ICurrentUser _currentUser;
    private readonly IWebHostEnvironment _env;

    public ProviderPendingCountViewComponent(
        IProviderDashboardService dashboardService,
        ICurrentUser currentUser,
        IWebHostEnvironment env)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
        _env = env;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        int providerId = (_currentUser.IsAuthenticated && _currentUser.UserId.HasValue)
            ? _currentUser.UserId.Value
            : (_env.IsDevelopment() ? 1 : 0);

        int count = 0;
        if (providerId > 0)
        {
            count = await _dashboardService.GetPendingCountAsync(providerId);
        }

        return View(count);
    }
}

