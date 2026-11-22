using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Web.Areas.Provider.Controllers;

public class DashboardController : ProviderAreaController
{
    private readonly IProviderDashboardService _dashboardService;
    private readonly IProviderBookingService _bookingService;

    public DashboardController(
        IProviderDashboardService dashboardService,
        IProviderBookingService bookingService,
        ICurrentUser currentUser,
        IWebHostEnvironment env) : base(currentUser, env)
    {
        _dashboardService = dashboardService;
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var vm = await _dashboardService.GetDashboardAsync(providerId, cancellationToken);
        if (vm == null) return NotFound("Provider profile not found.");

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int bookingId, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var result = await _bookingService.AcceptAsync(providerId, bookingId, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = $"Booking #{bookingId} accepted successfully!";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int bookingId, string reason, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var result = await _bookingService.RejectAsync(providerId, bookingId, reason, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = $"Booking #{bookingId} was declined.";
        }

        return RedirectToAction(nameof(Index));
    }
}

