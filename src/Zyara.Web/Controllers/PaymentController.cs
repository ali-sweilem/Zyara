using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Payments;

namespace Zyara.Web.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int bookingId)
        {
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