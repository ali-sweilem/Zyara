using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Bookings.Interfaces;

namespace Zyara.Web.Controllers;

public class CardPaymentController : Controller
{
    private readonly IBookingService _bookingService;

    public CardPaymentController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int bookingId)
    {
        var booking = await _bookingService.GetBookingAsync(bookingId);
        if (booking == null) return NotFound();

        if (booking.Status == BookingStatus.PendingPayment)
        {
            return RedirectToAction("Pay", "Booking", new { bookingId });
        }

        return RedirectToAction("Confirmation", "Booking", new { id = bookingId });
    }

    [HttpGet]
    public IActionResult Return(string reference, bool success = true)
    {
        return RedirectToAction("Index", "Home");
    }
}