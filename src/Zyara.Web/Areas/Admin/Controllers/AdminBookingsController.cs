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
        public async Task<IActionResult> Index(BookingStatus? status, int page = 1, CancellationToken cancellationToken = default)
        {
            ViewData["Status"] = status;   // عشان الفلتر يفضل مختار
            return View(await _bookings.GetBookingsAsync(status, page, cancellationToken: cancellationToken));
        }
    }
}