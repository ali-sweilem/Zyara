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
    ICareRecipientService careRecipients,
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

    // GET /Patient/Settings
    [HttpGet]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
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
        if (!ModelState.IsValid) return Failed(FirstError("Please check the passwords."), nameof(Settings));

        var result = await profile.ChangePasswordAsync(currentUser.RequireUserId(), dto, cancellationToken);
        return Done(result.Succeeded, result.Error, "Password changed. Your other devices were signed out.", nameof(Settings));
    }

    // ───────── patients on my account (CareRecipients) ─────────

    // POST /Patient/AddCareRecipient
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCareRecipient(AddCareRecipientDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var err = FirstError("Please check the patient details.");
            return IsAjaxRequest() ? BadRequest(new { error = err }) : Failed(err);
        }

        var result = await careRecipients.AddAsync(currentUser.RequireUserId(), dto, cancellationToken);
        if (IsAjaxRequest())
        {
            if (!result.Succeeded) return BadRequest(new { error = result.Error });
            TempData["Success"] = "Patient added.";
            return Ok(new { succeeded = true });
        }

        return Done(result.Succeeded, result.Error, "Patient added.");
    }

    // POST /Patient/UpdateCareRecipient
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCareRecipient(EditCareRecipientDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var err = FirstError("Please check the patient details.");
            return IsAjaxRequest() ? BadRequest(new { error = err }) : Failed(err);
        }

        var result = await careRecipients.UpdateAsync(currentUser.RequireUserId(), dto.Id, dto, cancellationToken);
        if (IsAjaxRequest())
        {
            if (!result.Succeeded) return BadRequest(new { error = result.Error });
            TempData["Success"] = "Patient updated.";
            return Ok(new { succeeded = true });
        }

        return Done(result.Succeeded, result.Error, "Patient updated.");
    }

    private bool IsAjaxRequest() =>
        string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    // POST /Patient/DeleteCareRecipient
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCareRecipient(int id, CancellationToken cancellationToken)
    {
        var result = await careRecipients.DeleteAsync(currentUser.RequireUserId(), id, cancellationToken);
        return Done(result.Succeeded, result.Error, "Patient removed.");
    }

    private IActionResult Done(bool succeeded, string? error, string successMessage, string actionName = nameof(Profile))
    {
        TempData[succeeded ? "Success" : "Error"] = succeeded ? successMessage : error;
        return RedirectToAction(actionName);
    }

    private IActionResult Failed(string message, string actionName = nameof(Profile))
    {
        TempData["Error"] = message;
        return RedirectToAction(actionName);
    }

    private string FirstError(string fallback) =>
        ModelState.Values.SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? fallback;
}
