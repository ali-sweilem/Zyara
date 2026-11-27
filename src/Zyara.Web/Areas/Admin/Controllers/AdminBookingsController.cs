using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Administration.Interfaces;

namespace Zyara.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminBookingsController : Controller
    {
        private readonly IAdminBookingService _bookings;

        public AdminBookingsController(IAdminBookingService bookings) => _bookings = bookings;

        [HttpGet]
        public async Task<IActionResult> Index(BookingStatus? status, string? search = null, int page = 1, CancellationToken cancellationToken = default)
        {
            ViewData["Status"] = status;
            ViewData["Search"] = search;
            return View(await _bookings.GetBookingsAsync(status, search, page, cancellationToken: cancellationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, BookingStatus? status, string? search = null, int page = 1, CancellationToken cancellationToken = default)
        {
            var result = await _bookings.CancelBookingAsync(id, cancellationToken);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? $"Booking #{id} cancelled." : result.Error;
            return RedirectToAction(nameof(Index), new { status, search, page });
        }
    }
}
