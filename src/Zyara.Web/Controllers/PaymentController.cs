using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Web.Controllers;

[Authorize(Policy = PolicyNames.PatientOnly)]
public class PaymentController : Controller
{
    private readonly IBookingService _bookingService;

    public PaymentController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int bookingId)
    {
        var booking = await _bookingService.GetBookingAsync(bookingId);
        if (booking is null)
        {
            return NotFound();
        }

        if (booking.Status == BookingStatus.PendingPayment)
        {
            return RedirectToAction("Pay", "Booking", new { bookingId });
        }

        return RedirectToAction("Confirmation", "Booking", new { id = bookingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int bookingId, string? dummy)
    {
        var booking = await _bookingService.GetBookingAsync(bookingId);
        if (booking is null)
        {
            return NotFound();
        }

        if (booking.Status == BookingStatus.PendingPayment)
        {
            return RedirectToAction("Pay", "Booking", new { bookingId });
        }

        return RedirectToAction("Confirmation", "Booking", new { id = bookingId });
    }
}
