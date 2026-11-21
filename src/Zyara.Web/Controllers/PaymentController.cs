using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Web.Controllers
{
    [Authorize(Policy = PolicyNames.PatientOnly)]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IBookingService _bookingService;
        private readonly IAuthorizationService _auth;

        public PaymentController(
            IPaymentService paymentService,
            IBookingService bookingService,
            IAuthorizationService auth)
        {
            _paymentService = paymentService;
            _bookingService = bookingService;
            _auth = auth;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int bookingId)
        {
            var booking = await _bookingService.GetBookingAsync(bookingId);
            if (booking is null)
            {
                return NotFound();
            }

            var authorization = await _auth.AuthorizeAsync(User, booking, Operations.Manage);
            if (!authorization.Succeeded)
            {
                return NotFound();
            }

            var dto = await _paymentService.GetPaymentDetailsAsync(bookingId);
            if (dto == null)
            {
                return NotFound($"Booking with ID #{bookingId} was not found.");
            }

            return View(dto);
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

            var authorization = await _auth.AuthorizeAsync(User, booking, Operations.Manage);
            if (!authorization.Succeeded)
            {
                return NotFound();
            }

            bool success = await _paymentService.AddPaymentAsync(bookingId);
            if (!success)
            {
                return NotFound("Could not process payment for this booking.");
            }

            TempData["Message"] = "Payment confirmed successfully via Cash on Arrival.";

            // التوجيه لصفحة الـ Confirmation في BookingController
            return RedirectToAction("Confirmation", "Booking", new { id = bookingId });
        }
    }
}
