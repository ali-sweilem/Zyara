using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Patients;

namespace Zyara.Web.Controllers;

[Authorize(Policy = PolicyNames.PatientOnly)]
public sealed class PatientController(IPatientPortalService portal, ICurrentUser currentUser) : Controller
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int bookingId, int stars, string? comment, CancellationToken cancellationToken)
    {
        var result = await portal.AddReviewAsync(currentUser.RequireUserId(), bookingId, stars, comment, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Thank you for your review." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
