using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Payments.Dtos;
using Zyara.Services.Features.Payments.Interface;
using Zyara.Services.Features.Patients;
using Zyara.Services.Features.Patients.Dtos;
using Zyara.Services.Features.Providers.Interfaces;
using Zyara.Services.Features.Shared.Dtos;

namespace Zyara.Web.Controllers;

[Authorize(Policy = PolicyNames.PatientOnly)]
public sealed class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IProviderDetailsService _providerDetailsService;
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationService _auth;
    private readonly ICardPaymentService _cardPaymentService;
    private readonly IPatientProfileService _patientProfileService;
    private readonly IHostedCheckoutSimulator? _simulator;

    public BookingController(
        IBookingService bookingService,
        IProviderDetailsService providerDetailsService,
        IPaymentService paymentService,
        ICurrentUser currentUser,
        IAuthorizationService auth,
        ICardPaymentService cardPaymentService,
        IPatientProfileService patientProfileService,
        IHostedCheckoutSimulator? simulator = null)
    {
        _bookingService = bookingService;
        _providerDetailsService = providerDetailsService;
        _paymentService = paymentService;
        _currentUser = currentUser;
        _auth = auth;
        _cardPaymentService = cardPaymentService;
        _patientProfileService = patientProfileService;
        _simulator = simulator;
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /Booking/Book?providerId=5&slotId=12&serviceId=3
    // ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Book(int providerId, int? slotId, int? serviceId, CancellationToken cancellationToken)
    {
        var vm = await BuildViewModelAsync(providerId, cancellationToken);
        if (vm is null)
        {
            return NotFound();
        }

        if (slotId.HasValue && vm.AvailableSlots.Any(s => s.Id == slotId.Value))
        {
            vm.SelectedTimeSlotId = slotId.Value;
        }
        else if (vm.SelectedTimeSlotId == 0 && vm.AvailableSlots.Count > 0)
        {
            vm.SelectedTimeSlotId = vm.AvailableSlots[0].Id;
        }

        if (serviceId.HasValue && vm.ProviderServices.Any(s => s.Id == serviceId.Value))
        {
            vm.SelectedProviderServiceId = serviceId.Value;
        }
        else if (vm.SelectedProviderServiceId == 0 && vm.ProviderServices.Count > 0)
        {
            vm.SelectedProviderServiceId = vm.ProviderServices[0].Id;
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

        var isCard = string.Equals(form.PaymentMethod, "Card", StringComparison.OrdinalIgnoreCase);

        if (isCard)
        {
            if (string.IsNullOrWhiteSpace(form.CardNumber))
            {
                ModelState.AddModelError(nameof(form.CardNumber), "Please enter your card number.");
            }
            else
            {
                var cleanCard = new string(form.CardNumber.Where(char.IsDigit).ToArray());
                if (cleanCard.Length < 16)
                {
                    ModelState.AddModelError(nameof(form.CardNumber), "Card number must be 16 digits (demo card: 4242 4242 4242 4242).");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            // Repopulate read-only fields before returning the view
            var refreshed = await BuildViewModelAsync(providerId, cancellationToken);
            if (refreshed is null)
            {
                return NotFound();
            }

            // Keep the patient's selections
            refreshed.SelectedCareRecipientId = form.SelectedCareRecipientId;
            refreshed.SelectedTimeSlotId = form.SelectedTimeSlotId;
            refreshed.SelectedProviderServiceId = form.SelectedProviderServiceId;
            refreshed.SelectedAddressId = form.SelectedAddressId;
            refreshed.CustomAddress = form.CustomAddress;
            refreshed.Notes = form.Notes;
            refreshed.PaymentMethod = form.PaymentMethod;
            refreshed.CardHolderName = form.CardHolderName;
            refreshed.CardNumber = form.CardNumber;
            refreshed.CardExpiry = form.CardExpiry;
            refreshed.CardCvv = form.CardCvv;

            return View(refreshed);
        }

        var patientId = _currentUser.RequireUserId();

        // Format visit address into notes so the provider knows where to go
        string? addressInfo = null;
        if (form.SelectedAddressId.HasValue && form.SelectedAddressId.Value > 0)
        {
            var profile = await _patientProfileService.GetAsync(patientId, cancellationToken);
            var addr = profile?.Addresses.FirstOrDefault(a => a.Id == form.SelectedAddressId.Value);
            if (addr != null)
            {
                addressInfo = $"Visit Location: {addr.Label} ({addr.StreetAndBuilding}, {addr.District}, {addr.City})";
            }
        }
        else if (!string.IsNullOrWhiteSpace(form.CustomAddress))
        {
            addressInfo = $"Visit Location: {form.CustomAddress.Trim()}";
        }

        string? finalNotes = form.Notes;
        if (!string.IsNullOrWhiteSpace(addressInfo))
        {
            finalNotes = string.IsNullOrWhiteSpace(finalNotes)
                ? addressInfo
                : $"{addressInfo} | {finalNotes}";
            if (finalNotes.Length > 500)
            {
                finalNotes = finalNotes.Substring(0, 500);
            }
        }

        var dto = new BookAppointmentDto
        {
            ProviderId = providerId,
            PatientId = patientId,
            TimeSlotId = form.SelectedTimeSlotId,
            ProviderServiceId = form.SelectedProviderServiceId,
            SelectedCareRecipientId = form.SelectedCareRecipientId,
            Notes = finalNotes,
            PaymentMethod = form.PaymentMethod,
            CardHolderName = form.CardHolderName,
            CardNumber = form.CardNumber,
            CardExpiry = form.CardExpiry,
            CardCvv = form.CardCvv
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

            refreshed.SelectedCareRecipientId = form.SelectedCareRecipientId;
            refreshed.SelectedTimeSlotId = form.SelectedTimeSlotId;
            refreshed.SelectedProviderServiceId = form.SelectedProviderServiceId;
            refreshed.SelectedAddressId = form.SelectedAddressId;
            refreshed.CustomAddress = form.CustomAddress;
            refreshed.Notes = form.Notes;
            refreshed.PaymentMethod = form.PaymentMethod;
            refreshed.CardHolderName = form.CardHolderName;
            refreshed.CardNumber = form.CardNumber;
            refreshed.CardExpiry = form.CardExpiry;
            refreshed.CardCvv = form.CardCvv;

            return View(refreshed);
        }

        var bookingId = result.Value;

        // Process payment based on chosen method
        if (isCard)
        {
            var returnUrl = Url.Action("Return", "CardPayment", new { bookingId }, Request.Scheme)!;
            var checkout = await _cardPaymentService.StartCheckoutAsync(bookingId, returnUrl);

            if (checkout is not null && _simulator is not null)
            {
                var (body, signature) = _simulator.Charge(checkout.Reference, form.CardNumber ?? string.Empty);
                var outcome = await _cardPaymentService.HandleWebhookAsync(body, signature);

                if (outcome == WebhookOutcome.Applied)
                {
                    TempData["Message"] = "Your appointment has been booked and payment was successfully processed!";
                    return RedirectToAction(nameof(Confirmation), new { id = bookingId });
                }
                else
                {
                    TempData["Error"] = "Payment was not authorized. Please verify your card details or choose cash on arrival.";
                    return RedirectToAction("Return", "CardPayment", new { bookingId });
                }
            }
            else if (checkout is not null)
            {
                return Redirect(checkout.RedirectUrl);
            }
        }

        // Cash on Arrival flow
        await _paymentService.AddPaymentAsync(bookingId);
        TempData["Message"] = "Your appointment request has been submitted successfully (Cash on Arrival)!";
        return RedirectToAction(nameof(Confirmation), new { id = bookingId });
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /Booking/Confirmation?id=12
    // (called by PaymentController after successful payment confirmation)
    // ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id, CancellationToken cancellationToken)
    {
        var booking = await _bookingService.GetBookingAsync(id, cancellationToken);
        if (booking is null)
        {
            return NotFound();
        }

        var authorization = await _auth.AuthorizeAsync(User, booking, Operations.View);
        if (!authorization.Succeeded)
        {
            return NotFound();
        }

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

        var patientId = _currentUser.RequireUserId();
        var profile = await _patientProfileService.GetAsync(patientId, cancellationToken);
        var addresses = profile?.Addresses ?? Array.Empty<PatientAddressItemDto>();
        var recipients = profile?.CareRecipients ?? Array.Empty<Zyara.Services.Features.Patients.Dtos.CareRecipientDto>();

        return new BookAppointmentDto
        {
            PatientId = patientId,
            ProviderId = provider.Id,
            ProviderFullName = provider.FullName,
            ProviderSpecialty = provider.Specialty,
            ProviderPhotoUrl = provider.PhotoUrl,
            ProviderBaseCity = provider.BaseCity,
            AvailableSlots = slots,
            ProviderServices = provider.Services,
            PatientAddresses = addresses,
            SelectedAddressId = addresses.FirstOrDefault()?.Id,
            CareRecipients = recipients,
            SelectedCareRecipientId = recipients.FirstOrDefault()?.Id ?? 0
        };
    }
}



