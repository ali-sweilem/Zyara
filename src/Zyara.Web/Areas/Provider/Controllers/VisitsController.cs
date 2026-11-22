using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Web.Areas.Provider.Controllers;

public class VisitsController : ProviderAreaController
{
    private readonly IProviderBookingService _bookingService;

    public VisitsController(
        IProviderBookingService bookingService,
        ICurrentUser currentUser,
        IWebHostEnvironment env) : base(currentUser, env)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string group = "upcoming",
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var pagedVisits = await _bookingService.GetVisitsAsync(
            providerId, group, status, fromDate, toDate, page, 10, cancellationToken);

        ViewBag.ActiveGroup = group;
        ViewBag.StatusFilter = status;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        return View(pagedVisits);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var details = await _bookingService.GetVisitDetailsAsync(providerId, id, cancellationToken);
        if (details == null) return NotFound("Visit not found or access denied.");

        return View(details);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int bookingId, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var result = await _bookingService.AcceptAsync(providerId, bookingId, cancellationToken);
        if (!result.Succeeded) TempData["Error"] = result.Error;
        else TempData["Success"] = $"Booking #{bookingId} accepted!";

        return RedirectToAction(nameof(Details), new { id = bookingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int bookingId, string reason, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var result = await _bookingService.RejectAsync(providerId, bookingId, reason, cancellationToken);
        if (!result.Succeeded) TempData["Error"] = result.Error;
        else TempData["Success"] = $"Booking #{bookingId} declined.";

        return RedirectToAction(nameof(Details), new { id = bookingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int bookingId, string? otp, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var result = await _bookingService.StartAsync(providerId, bookingId, otp, cancellationToken);
        if (!result.Succeeded) TempData["Error"] = result.Error;
        else TempData["Success"] = $"Visit #{bookingId} started!";

        return RedirectToAction(nameof(Details), new { id = bookingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int bookingId, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var result = await _bookingService.CompleteAsync(providerId, bookingId, cancellationToken);
        if (!result.Succeeded) TempData["Error"] = result.Error;
        else TempData["Success"] = $"Visit #{bookingId} marked as completed!";

        return RedirectToAction(nameof(Details), new { id = bookingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int bookingId, string reason, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var result = await _bookingService.CancelAsync(providerId, bookingId, reason, cancellationToken);
        if (!result.Succeeded) TempData["Error"] = result.Error;
        else TempData["Success"] = $"Booking #{bookingId} cancelled.";

        return RedirectToAction(nameof(Details), new { id = bookingId });
    }
}

