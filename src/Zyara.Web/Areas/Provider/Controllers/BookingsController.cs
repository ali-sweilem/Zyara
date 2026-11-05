using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Bookings;
using Zyara.Web.Areas.Provider.ViewModels;

namespace Zyara.Web.Areas.Provider.Controllers;

[Area("Provider")]
public class BookingsController : Controller
{
    // TODO(auth): add [Authorize(Roles = "Doctor,Nurse")] once cookie authentication is configured

    private readonly IBookingService _bookingService;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public BookingsController(IBookingService bookingService, IConfiguration config, IWebHostEnvironment env)
    {
        _bookingService = bookingService;
        _config = config;
        _env = env;
    }

    private int? GetCurrentProviderId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out var id))
            return id;

        // TEMP-DEV: fallback for development without auth configured
        if (_env.IsDevelopment())
        {
            if (int.TryParse(_config["Dev:ProviderId"], out var dev))
                return dev;
        }

        return null;
    }

    public async Task<IActionResult> Index(BookingFilterViewModel? filter)
    {
        var providerId = GetCurrentProviderId();
        if (providerId == null) return Unauthorized();

        var f = filter ?? new BookingFilterViewModel();
        var serviceFilter = new BookingFilter
        {
            Status = f.Status,
            PaymentMethod = f.PaymentMethod,
            ServiceType = f.ServiceType,
            DateFrom = f.DateFrom,
            DateTo = f.DateTo,
            SearchText = f.SearchText,
            Page = f.Page,
            PageSize = f.PageSize
        };

        var result = await _bookingService.GetProviderBookingsAsync(providerId.Value, serviceFilter);
        var vm = new BookingListViewModel
        {
            Filter = f,
            Items = result.Items.Select(b => new BookingListItemViewModel
            {
                Id = b.Id,
                PatientName = b.Patient?.FullName ?? "N/A",
                PatientPhone = b.Patient?.Phone ?? "N/A",
                ServiceName = b.Service?.Name ?? "N/A",
                Date = b.TimeSlot?.Date ?? default,
                StartTime = b.TimeSlot?.StartTime ?? default,
                EndTime = b.TimeSlot?.EndTime ?? default,
                Status = b.Status.ToString(),
                TotalPrice = b.TotalPrice,
                PaymentMethod = b.Payment?.Method.ToString() ?? "N/A"
            }).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var providerId = GetCurrentProviderId();
        if (providerId == null) return Unauthorized();

        var result = await _bookingService.GetBookingDetailsAsync(providerId.Value, id);
        if (!result.Succeeded || result.Value == null) return NotFound();
        var b = result.Value.Booking;
        var notes = result.Value.ServiceNotes;

        var vm = new BookingDetailsViewModel
        {
            Id = b.Id,
            PatientName = b.Patient?.FullName ?? "N/A",
            PatientPhone = b.Patient?.Phone ?? "N/A",
            ServiceName = b.Service?.Name ?? "N/A",
            Date = b.TimeSlot?.Date ?? default,
            StartTime = b.TimeSlot?.StartTime ?? default,
            EndTime = b.TimeSlot?.EndTime ?? default,
            Status = b.Status.ToString(),
            ServiceFee = b.ServiceFee,
            TotalPrice = b.TotalPrice,
            Commission = b.Commission,
            PaymentMethod = b.Payment?.Method.ToString() ?? "N/A",
            PaymentStatus = b.Payment?.Status.ToString() ?? "N/A",
            Address = b.Address != null ? $"{b.Address.StreetAndBuilding}, {b.Address.City}" : "N/A",
            RatingStars = b.Rating?.Stars,
            RatingComment = b.Rating?.Comment,
            ServiceNotes = notes.Select(s => new ServiceNoteViewModel { Id = s.Id, Content = s.Content }).ToList(),
            IsHomeVisit = b.Service?.Type == ServiceType.DoctorHomeVisit || b.Service?.Type == ServiceType.NurseHomeVisit,
            CanAccept = b.Status == BookingStatus.PendingProviderApproval,
            CanReject = b.Status == BookingStatus.PendingProviderApproval,
            CanStart = b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.ConfirmedCollectionPending,
            CanComplete = b.Status == BookingStatus.Started,
            CanCancel = b.Status == BookingStatus.PendingProviderApproval || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.ConfirmedCollectionPending,
            CanAddNote = b.Status == BookingStatus.Started || b.Status == BookingStatus.Completed
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id)
    {
        var pid = GetCurrentProviderId();
        if (pid == null) return Unauthorized();
        var res = await _bookingService.AcceptAsync(pid.Value, id);
        if (res.Succeeded) TempData["Success"] = "Booking accepted.";
        else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var pid = GetCurrentProviderId();
        if (pid == null) return Unauthorized();
        var res = await _bookingService.RejectAsync(pid.Value, id);
        if (res.Succeeded) TempData["Success"] = "Booking rejected.";
        else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id, string otp)
    {
        var pid = GetCurrentProviderId();
        if (pid == null) return Unauthorized();
        var res = await _bookingService.StartAsync(pid.Value, id, otp ?? string.Empty);
        if (res.Succeeded) TempData["Success"] = "Booking started.";
        else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var pid = GetCurrentProviderId();
        if (pid == null) return Unauthorized();
        var res = await _bookingService.CompleteAsync(pid.Value, id);
        if (res.Succeeded) TempData["Success"] = "Booking completed.";
        else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var pid = GetCurrentProviderId();
        if (pid == null) return Unauthorized();
        var res = await _bookingService.CancelAsync(pid.Value, id);
        if (res.Succeeded) TempData["Success"] = "Booking cancelled.";
        else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(AddServiceNoteViewModel model)
    {
        var pid = GetCurrentProviderId();
        if (pid == null) return Unauthorized();
        if (!ModelState.IsValid) { TempData["Error"] = "Note is required and must not exceed the allowed length."; return RedirectToAction(nameof(Details), new { id = model.BookingId }); }
        var res = await _bookingService.AddServiceNoteAsync(pid.Value, model.BookingId, model.Content);
        if (res.Succeeded) TempData["Success"] = "Note added.";
        else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Details), new { id = model.BookingId });
    }
}
