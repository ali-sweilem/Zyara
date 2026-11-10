using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Providers;
using Zyara.Web.Models;

namespace Zyara.Web.Controllers;

public sealed class ProvidersController : Controller
{
    private readonly IProviderDetailsService _providerDetailsService;
    private readonly IProviderDashboardService _dashboardService;

    public ProvidersController(IProviderDetailsService providerDetailsService , IProviderDashboardService dashboardService)
    {
        _providerDetailsService = providerDetailsService;
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, int reviewsPage = 1, CancellationToken cancellationToken = default)
    {
        var provider = await _providerDetailsService.GetDetailsAsync(id, cancellationToken);
        if (provider is null)
        {
            return NotFound();
        }

        var reviews = await _providerDetailsService.GetReviewsAsync(id, reviewsPage, 5, cancellationToken);
        return View(new ProviderDetailsViewModel
        {
            Provider = provider,
            Reviews = reviews
        });
    }
  
    // /Providers/Dashboard
    public async Task<IActionResult> Dashboard(int providerId = 1, CancellationToken cancellationToken = default)
    {
        var dto = await _dashboardService.GetDashboardAsync(providerId, cancellationToken);

        var vm = new ProviderDashboardViewModel
        {
            ProviderId = providerId,
            TotalBookings = dto.TotalBookings,
            PendingRequests = dto.PendingRequests,
            UpcomingBookings = dto.UpcomingBookings,
            CompletedBookings = dto.CompletedBookings,
            TotalEarnings = dto.TotalEarnings,
            PendingList = dto.PendingList,
            CompletedList = dto.CompletedList
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int providerId, int bookingId, CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.AcceptBookingAsync(providerId, bookingId, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Request accepted." : result.Error;
        return RedirectToAction(nameof(Dashboard), new { providerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int providerId, int bookingId, CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.RejectBookingAsync(providerId, bookingId, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Request rejected." : result.Error;
        return RedirectToAction(nameof(Dashboard), new { providerId });
    }
}
