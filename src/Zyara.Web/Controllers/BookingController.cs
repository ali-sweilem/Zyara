using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Services.Common;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Payments.Dtos;
using Zyara.Services.Features.Payments.Interface;
using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Web.Controllers;

public sealed class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IProviderDetailsService _providerDetailsService;
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUser _currentUser;
    private readonly IWebHostEnvironment _env;
    private readonly ZyaraDbContext _db;
    private readonly TimeProvider _timeProvider;

    public BookingController(
        IBookingService bookingService,
        IProviderDetailsService providerDetailsService,
        IPaymentService paymentService,
        ICurrentUser currentUser,
        IWebHostEnvironment env,
        ZyaraDbContext db,
        TimeProvider timeProvider)
    {
        _bookingService = bookingService;
        _providerDetailsService = providerDetailsService;
        _paymentService = paymentService;
        _currentUser = currentUser;
        _env = env;
        _db = db;
        _timeProvider = timeProvider;
    }

    private int? CurrentPatientId =>
        (_currentUser.IsAuthenticated && _currentUser.UserId.HasValue)
            ? _currentUser.UserId.Value
            : (_env.IsDevelopment() ? 1 : null);

    // ─────────────────────────────────────────────────────────────────
    // GET /Booking/Book?providerId=5&serviceId=2&slotId=14
    // ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Book(int providerId, int? serviceId, int? slotId, CancellationToken cancellationToken)
    {
        var patientId = CurrentPatientId;
        if (!patientId.HasValue) return Challenge();

        var vm = await BuildViewModelAsync(providerId, patientId.Value, serviceId, slotId, cancellationToken);
        if (vm is null)
        {
            return NotFound("Provider not found or not accepting bookings.");
        }

        return View(vm);
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /Booking/Book
    // ─────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookAppointmentPostModel form, CancellationToken cancellationToken)
    {
        var patientId = CurrentPatientId;
        if (!patientId.HasValue) return Challenge();

        if (!ModelState.IsValid)
        {
            var refreshed = await BuildViewModelAsync(form.ProviderId, patientId.Value, form.SelectedProviderServiceId, form.SelectedTimeSlotId, cancellationToken);
            if (refreshed is null) return NotFound();

            refreshed.Form = form;
            return View(refreshed);
        }

        var command = new CreateBookingCommand
        {
            PatientId = patientId.Value,
            ProviderId = form.ProviderId,
            ProviderServiceId = form.SelectedProviderServiceId,
            TimeSlotId = form.SelectedTimeSlotId,
            PaymentMethod = form.PaymentMethod,
            Notes = form.Notes,
            AddressId = form.AddressId,
            Label = form.Label,
            City = form.City,
            District = form.District,
            StreetAndBuilding = form.StreetAndBuilding,
            Latitude = form.Latitude,
            Longitude = form.Longitude,
            SaveNewAddress = form.SaveNewAddress
        };

        var result = await _bookingService.CreateBookingAsync(command, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            var refreshed = await BuildViewModelAsync(form.ProviderId, patientId.Value, form.SelectedProviderServiceId, form.SelectedTimeSlotId, cancellationToken);
            if (refreshed is null) return NotFound();

            refreshed.Form = form;
            return View(refreshed);
        }

        if (result.Value.NeedsPayment)
        {
            return RedirectToAction(nameof(Pay), new { bookingId = result.Value.BookingId });
        }

        TempData["Success"] = "Your appointment request has been submitted successfully!";
        return RedirectToAction(nameof(Confirmation), new { id = result.Value.BookingId });
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /Booking/Pay?bookingId=12
    // ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Pay(int bookingId, CancellationToken cancellationToken)
    {
        var patientId = CurrentPatientId;
        if (!patientId.HasValue) return Challenge();

        var summary = await _paymentService.GetBookingPaymentSummaryAsync(bookingId, patientId.Value, cancellationToken);
        if (summary is null) return NotFound("Booking not found or access denied.");

        if (summary.PaymentStatus == PaymentStatus.Authorized || summary.PaymentStatus == PaymentStatus.Captured)
        {
            return RedirectToAction(nameof(Confirmation), new { id = bookingId });
        }

        ViewBag.Summary = summary;
        var input = new CardPayInputModel { BookingId = bookingId };
        return View(input);
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /Booking/Pay
    // ─────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(CardPayInputModel form, CancellationToken cancellationToken)
    {
        var patientId = CurrentPatientId;
        if (!patientId.HasValue) return Challenge();

        var summary = await _paymentService.GetBookingPaymentSummaryAsync(form.BookingId, patientId.Value, cancellationToken);
        if (summary is null) return NotFound();

        ViewBag.Summary = summary;

        if (!ModelState.IsValid)
        {
            // Clear card number and cvv
            form.CardNumber = string.Empty;
            form.Cvv = string.Empty;
            return View(form);
        }

        var cardInput = new CardInput(
            form.CardHolderName,
            form.CardNumber,
            form.ExpiryMonth,
            form.ExpiryYear,
            form.Cvv);

        var payResult = await _paymentService.PayWithCardAsync(patientId.Value, form.BookingId, cardInput, cancellationToken);

        // Security: clear sensitive card details immediately
        form.CardNumber = string.Empty;
        form.Cvv = string.Empty;

        if (!payResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, payResult.Error!);
            return View(form);
        }

        TempData["Success"] = "Pre-authorization hold placed successfully. Your request has been sent to the provider.";
        return RedirectToAction(nameof(Confirmation), new { id = form.BookingId });
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /Booking/Confirmation?id=12
    // ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Confirmation(int id, int? bookingId, CancellationToken cancellationToken)
    {
        var patientId = CurrentPatientId;
        if (!patientId.HasValue) return Challenge();

        var targetId = id > 0 ? id : (bookingId ?? 0);
        var confirmation = await _paymentService.GetBookingConfirmationAsync(targetId, cancellationToken);
        if (confirmation is null) return NotFound();

        if (confirmation.PatientId != patientId.Value)
        {
            return NotFound();
        }

        if (confirmation.Status == BookingStatus.PendingPayment)
        {
            return RedirectToAction(nameof(Pay), new { bookingId = targetId });
        }

        return View(confirmation);
    }

    // ─────────────────────────────────────────────────────────────────
    // Private Helpers
    // ─────────────────────────────────────────────────────────────────
    private async Task<BookAppointmentViewModel?> BuildViewModelAsync(
        int providerId,
        int patientId,
        int? preselectedServiceId,
        int? preselectedSlotId,
        CancellationToken cancellationToken)
    {
        var provider = await _providerDetailsService.GetDetailsAsync(providerId, cancellationToken);
        if (provider is null) return null;

        var slots = await _bookingService.GetAvailableSlotsAsync(providerId, cancellationToken);
        var savedAddresses = await _bookingService.GetPatientAddressesAsync(patientId, cancellationToken);

        var settings = await _db.PlatformSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new PlatformSettings();

        var egyptNow = EgyptTime.Now(_timeProvider);

        var days = slots
            .GroupBy(s => s.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new BookAppointmentDayDto
            {
                Date = g.Key,
                DayLabel = SlotFormatting.FormatDayLabel(g.Key, egyptNow),
                WeekdayName = g.Key.ToString("ddd"),
                DayNumber = g.Key.Day.ToString(),
                MonthName = g.Key.ToString("MMM"),
                Windows = g.Select(s => new BookAppointmentWindowDto
                {
                    Id = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    FormattedWindow = SlotFormatting.FormatWindow(s.StartTime, s.EndTime),
                    PartOfDay = SlotFormatting.GetPartOfDay(s.StartTime)
                }).ToList()
            })
            .ToList();

        var serviceDtos = provider.Services.Select(s => new BookAppointmentServiceItemDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Fee = s.Fee,
            Type = s.Type
        }).ToList();

        var chosenServiceId = preselectedServiceId ?? (serviceDtos.Count == 1 ? serviceDtos[0].Id : (int?)null);

        string? preselectedDay = null;
        if (preselectedSlotId.HasValue)
        {
            var matchedDay = days.FirstOrDefault(d => d.Windows.Any(w => w.Id == preselectedSlotId.Value));
            if (matchedDay != null) preselectedDay = matchedDay.DateKey;
        }
        if (preselectedDay == null && days.Count > 0)
        {
            preselectedDay = days[0].DateKey;
        }

        return new BookAppointmentViewModel
        {
            ProviderId = provider.Id,
            ProviderFullName = provider.FullName,
            ProviderSpecialty = provider.Specialty,
            ProviderPhotoUrl = provider.PhotoUrl,
            ProviderBaseCity = provider.BaseCity,
            PlatformServiceFee = settings.ServiceFee,
            Services = serviceDtos,
            Days = days,
            SavedAddresses = savedAddresses,
            PreselectedServiceId = chosenServiceId,
            PreselectedSlotId = preselectedSlotId,
            PreselectedDay = preselectedDay,
            Form = new BookAppointmentPostModel
            {
                ProviderId = provider.Id,
                SelectedProviderServiceId = chosenServiceId ?? 0,
                SelectedTimeSlotId = preselectedSlotId ?? 0,
                PaymentMethod = PaymentMethod.CashOnArrival,
                SaveNewAddress = true
            }
        };
    }
}
