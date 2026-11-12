
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Bookings;
using Zyara.Services.Features.Bookings.Interfaces;

namespace Zyara.Web.Areas.Patient.Controllers;

[Area("Patient")]
public class BookingsController : Controller
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // GET: Patient/Bookings
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var patientId = GetCurrentPatientId();

        var bookings = await _bookingService.GetMyBookingsAsync(
            patientId,
            cancellationToken);

        return View(bookings);
    }

    // GET: Patient/Bookings/Details/5
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        var patientId = GetCurrentPatientId();

        var booking = await _bookingService.GetMyBookingDetailsAsync(
            id,
            patientId,
            cancellationToken);

        if (booking is null)
            return NotFound();

        return View(booking);
    }

    // POST: Patient/Bookings/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        int id,
        CancellationToken cancellationToken)
    {
        var patientId = GetCurrentPatientId();

        var result = await _bookingService.CancelMyBookingAsync(
            id,
            patientId,
            cancellationToken);

        TempData["BookingMessage"] = result switch
        {
            BookingCancellationResult.Cancelled =>
                "Booking cancelled successfully.",

            BookingCancellationResult.NotFound =>
                "Booking not found.",

            BookingCancellationResult.NotCancellable =>
                "This booking cannot be cancelled.",

            _ => "Something went wrong."
        };

        return RedirectToAction(nameof(Index));
    }

    // POST: Patient/Bookings/Rate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(
        int id,
        int stars,
        string? comment,
        CancellationToken cancellationToken)
    {
        var patientId = GetCurrentPatientId();

        var result = await _bookingService.SubmitRatingAsync(
            id,
            patientId,
            stars,
            comment,
            cancellationToken);

        TempData["BookingMessage"] = result switch
        {
            ReviewSubmissionResult.Submitted =>
                "Your review has been submitted.",

            ReviewSubmissionResult.NotFound =>
                "Booking not found.",

            ReviewSubmissionResult.NotCompleted =>
                "You can only review completed bookings.",

            ReviewSubmissionResult.AlreadyReviewed =>
                "You have already reviewed this booking.",

            ReviewSubmissionResult.InvalidInput =>
                "Please enter a valid rating and comment.",

            _ => "Something went wrong."
        };

        return RedirectToAction(nameof(Details), new { id });
    }

    private int GetCurrentPatientId()
    {
        // TODO: Replace with the authenticated user's Patient ID
        // after the authentication system is implemented.

        throw new NotImplementedException(
            "Patient authentication has not been implemented yet.");
    }
}
