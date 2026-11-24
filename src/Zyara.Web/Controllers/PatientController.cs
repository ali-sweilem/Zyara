using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Patients;
using Zyara.Services.Features.Patients.Dtos;

namespace Zyara.Web.Controllers;

[Authorize(Policy = PolicyNames.PatientOnly)]
public sealed class PatientController(
    IPatientPortalService portal,
    IPatientProfileService profile,
    ICurrentUser currentUser) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await portal.GetDashboardAsync(currentUser.RequireUserId(), cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int bookingId, CancellationToken cancellationToken)
    {
        var result = await portal.CancelBookingAsync(currentUser.RequireUserId(), bookingId, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Booking cancelled." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ───────── profile (reviews live in ReviewsController) ─────────

    // GET /Patient/Profile
    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var model = await profile.GetAsync(currentUser.RequireUserId(), cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    // POST /Patient/UpdateProfile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(EditPatientProfileDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Failed(FirstError("Please check your details."));

        var result = await profile.UpdateAsync(currentUser.RequireUserId(), dto, cancellationToken);
        return Done(result.Succeeded, result.Error, "Profile updated.");
    }

    // POST /Patient/AddAddress
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(AddAddressDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Failed(FirstError("Please check the address."));

        var result = await profile.AddAddressAsync(currentUser.RequireUserId(), dto, cancellationToken);
        return Done(result.Succeeded, result.Error, "Address added.");
    }

    // POST /Patient/DeleteAddress
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id, CancellationToken cancellationToken)
    {
        var result = await profile.DeleteAddressAsync(currentUser.RequireUserId(), id, cancellationToken);
        return Done(result.Succeeded, result.Error, "Address deleted.");
    }

    // POST /Patient/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Failed(FirstError("Please check the passwords."));

        var result = await profile.ChangePasswordAsync(currentUser.RequireUserId(), dto, cancellationToken);
        return Done(result.Succeeded, result.Error, "Password changed. Your other devices were signed out.");
    }

    private IActionResult Done(bool succeeded, string? error, string successMessage)
    {
        TempData[succeeded ? "Success" : "Error"] = succeeded ? successMessage : error;
        return RedirectToAction(nameof(Profile));
    }

    private IActionResult Failed(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Profile));
    }

    private string FirstError(string fallback) =>
        ModelState.Values.SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? fallback;
}
