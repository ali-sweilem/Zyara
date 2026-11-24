using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Reviews.Dtos;
using Zyara.Services.Features.Reviews.Interfaces;

namespace Zyara.Web.Controllers;

/// <summary>
/// The patient's reviews: "My reviews" + the "Rate your visit" page shown after a completed visit.
/// The patient id ALWAYS comes from ICurrentUser, never from a form or the URL.
/// </summary>
[Authorize(Policy = PolicyNames.PatientOnly)]
public sealed class ReviewsController(IReviewService reviews, ICurrentUser currentUser) : Controller
{
    // GET /Reviews
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await reviews.GetPageAsync(currentUser.RequireUserId(), cancellationToken);
        return View(model);
    }

    // GET /Reviews/Create?bookingId=5
    [HttpGet]
    public async Task<IActionResult> Create(int bookingId, CancellationToken cancellationToken)
    {
        var form = await reviews.GetFormAsync(currentUser.RequireUserId(), bookingId, cancellationToken);
        if (!form.Succeeded)
        {
            TempData["Error"] = form.Error;
            return RedirectToAction(nameof(Index));
        }

        return View(form.Value);
    }

    // POST /Reviews/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewDto dto, CancellationToken cancellationToken)
    {
        var patientId = currentUser.RequireUserId();

        if (!ModelState.IsValid)
        {
            // show the page again with what the patient already typed
            var form = await reviews.GetFormAsync(patientId, dto.BookingId, cancellationToken);
            if (!form.Succeeded)
            {
                TempData["Error"] = form.Error;
                return RedirectToAction(nameof(Index));
            }

            form.Value!.Stars = dto.Stars;
            form.Value.Comment = dto.Comment;
            return View(form.Value);
        }

        var result = await reviews.SubmitAsync(patientId, dto, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Thank you! Your review was published.";
        return RedirectToAction(nameof(Index));
    }
}
