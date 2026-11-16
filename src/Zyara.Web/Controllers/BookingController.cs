using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Payments.Interface;
using Zyara.Services.Features.Providers.Interfaces;
using Zyara.Services.Features.Shared.Dtos;

namespace Zyara.Web.Controllers;

public sealed class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IProviderDetailsService _providerDetailsService;
    private readonly IPaymentService _paymentService;

    public BookingController(
        IBookingService bookingService,
        IProviderDetailsService providerDetailsService,
        IPaymentService paymentService)
    {
        _bookingService = bookingService;
        _providerDetailsService = providerDetailsService;
        _paymentService = paymentService;
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /Booking/Book?providerId=5
    // ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Book(int providerId, CancellationToken cancellationToken)
    {
        var vm = await BuildViewModelAsync(providerId, cancellationToken);
        if (vm is null)
        {
            return NotFound();
        }

        return View(vm);
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /Booking/Book
    // ─────────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookAppointmentDto form, CancellationToken cancellationToken)
    {
        // --- Security: take providerId from the form's display field (not trusted for
        //               anything financial), validate it against the DB in the service.
        var providerId = form.ProviderId;

        if (!ModelState.IsValid)
        {
            // Repopulate read-only fields before returning the view
            var refreshed = await BuildViewModelAsync(providerId, cancellationToken);
            if (refreshed is null)
            {
                return NotFound();
            }

            // Keep the patient's selections
            refreshed.SelectedTimeSlotId = form.SelectedTimeSlotId;
            refreshed.SelectedProviderServiceId = form.SelectedProviderServiceId;
            refreshed.Notes = form.Notes;

            return View(refreshed);
        }

        var patientId = GetCurrentPatientId();

        var dto = new BookAppointmentDto
        {
            ProviderId = providerId,
            PatientId = patientId,
            TimeSlotId = form.SelectedTimeSlotId,
            ProviderServiceId = form.SelectedProviderServiceId,
            Notes = form.Notes
        };

        var result = await _bookingService.CreateBookingAsync(dto, cancellationToken);

        if (!result.Succeeded)
        {
            // Service-level validation error (slot taken, provider not found, etc.)
            ModelState.AddModelError(string.Empty, result.Error!);

            var refreshed = await BuildViewModelAsync(providerId, cancellationToken);
            if (refreshed is null)
            {
                return NotFound();
            }

            refreshed.SelectedProviderServiceId = form.SelectedProviderServiceId;
            refreshed.Notes = form.Notes;

            return View(refreshed);
        }

        // PRG: redirect to Payment page so the patient can confirm cash-on-arrival payment
        TempData["Message"] = "Your appointment request has been submitted successfully!";
        return RedirectToAction("Create", "Payment", new { bookingId = result.Value });
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /Booking/Confirmation?id=12
    // (called by PaymentController after successful payment confirmation)
    // ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id, CancellationToken cancellationToken)
    {
        var dto = await _paymentService.GetBookingConfirmationAsync(id);
        if (dto is null)
        {
            return NotFound();
        }

        return View(dto);
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the current patient's ID from claims.
    /// Falls back to 1 when no authentication is configured (dev/demo mode).
    /// TODO: remove the fallback once a real login system is wired up.
    /// </summary>
    private int GetCurrentPatientId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 1;
    }

    /// <summary>
    /// Loads provider summary + available slots + services and builds the BookAppointmentDto.
    /// Returns null if the provider is not found.
    /// </summary>
    private async Task<BookAppointmentDto?> BuildViewModelAsync(
        int providerId,
        CancellationToken cancellationToken)
    {
        var provider = await _providerDetailsService.GetDetailsAsync(providerId, cancellationToken);
        if (provider is null)
        {
            return null;
        }

        var slots = await _bookingService.GetAvailableSlotsAsync(providerId, cancellationToken);

        return new BookAppointmentDto
        {
            ProviderId = provider.Id,
            ProviderFullName = provider.FullName,
            ProviderSpecialty = provider.Specialty,
            ProviderPhotoUrl = provider.PhotoUrl,
            ProviderBaseCity = provider.BaseCity,
            AvailableSlots = slots,
            ProviderServices = provider.Services
        };
    }
}



