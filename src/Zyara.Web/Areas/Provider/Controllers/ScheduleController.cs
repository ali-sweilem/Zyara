using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Web.Areas.Provider.Controllers;

public class ScheduleController : ProviderAreaController
{
    private readonly IProviderScheduleService _scheduleService;

    public ScheduleController(
        IProviderScheduleService scheduleService,
        ICurrentUser currentUser,
        IWebHostEnvironment env) : base(currentUser, env)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool includePast = false, CancellationToken cancellationToken = default)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var vm = await _scheduleService.GetScheduleAsync(providerId, includePast, cancellationToken);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(GenerateScheduleCommand command, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var result = await _scheduleService.GenerateAsync(providerId, command, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            var res = result.Value;
            var summary = $"Generated {res.Created} arrival window(s) successfully.";
            if (res.SkippedOverlap > 0) summary += $" ({res.SkippedOverlap} skipped due to overlap)";
            if (res.SkippedPast > 0) summary += $" ({res.SkippedPast} skipped in the past)";
            if (res.SkippedBeyondHorizon > 0) summary += $" ({res.SkippedBeyondHorizon} skipped beyond 30-day horizon)";
            if (res.DroppedPartial > 0) summary += $" ({res.DroppedPartial} trailing partial window dropped)";

            TempData["Success"] = summary;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSingle(AddSingleSlotCommand command, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var result = await _scheduleService.AddSingleAsync(providerId, command, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Time slot window added successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int slotId, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var result = await _scheduleService.DeleteAsync(providerId, slotId, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Time slot deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearDay(DateTime date, CancellationToken cancellationToken)
    {
        var authCheck = RequireProvider(out var providerId);
        if (authCheck != null) return authCheck;

        var result = await _scheduleService.ClearDayAsync(providerId, date, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = $"Cleared {result.Value} available window(s) for {date:ddd, d MMM yyyy}.";
        }

        return RedirectToAction(nameof(Index));
    }
}
