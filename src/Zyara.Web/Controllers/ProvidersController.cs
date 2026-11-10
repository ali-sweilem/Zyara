using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;
using Zyara.Web.Models;

namespace Zyara.Web.Controllers;


public sealed class ProvidersController : Controller
{
    private readonly IProviderDetailsService _providerDetailsService;
    private readonly IProviderDashboardService _dashboardService;
    private readonly IWebHostEnvironment _env;

    public ProvidersController(
        IProviderDetailsService providerDetailsService,
        IProviderDashboardService dashboardService,
        IWebHostEnvironment env)
    {
        _providerDetailsService = providerDetailsService;
        _dashboardService = dashboardService;
        _env = env;
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

    //==============================================profile==============================================
    //  /Providers/showprofile/5
    public async Task<IActionResult> showprofile(int id)
    {
        var model = await _providerDetailsService.showprofile(id);
        if (model == null)
        {
            return NotFound();
        }
        return View(model);
    }

    [HttpGet]

    public async Task<IActionResult> EditProfile(int id)
    {
        var model = await _providerDetailsService.GetForEditAsync(id);
        if (model == null)
        {
            return NotFound();
        }
        return View(model);
    }
  
    [HttpPost]
    public async Task<IActionResult> EditProfile(EditProfileProviderDto model)
    {
    
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        if(model.photofile is { Length: > 0 })
        {
            var ext = Path.GetExtension(model.photofile.FileName).ToLowerInvariant();
            var folder = Path.Combine(_env.WebRootPath, "uploads", "providers");
            Directory.CreateDirectory(folder);

            var filename = $"{Guid.NewGuid()}{ext}";
            using (var stream = new FileStream(Path.Combine(folder, filename), FileMode.Create))
            {
                await model.photofile.CopyToAsync(stream);
            }
                model.PhotoUrl = $"/uploads/Providers/{filename}";
        }
        var success = await _providerDetailsService.UpdateProfileAsync(model);
        if (!success)
        {
            return NotFound();
        }
        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(showprofile), new { id = model.Id });
    }

}

