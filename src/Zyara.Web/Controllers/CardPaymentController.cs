using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Web.Controllers
{
    public class CardPaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaymentGateway _paymentGateway;

        public CardPaymentController(IPaymentService paymentService, IPaymentGateway paymentGateway)
        {
            _paymentService = paymentService;
            _paymentGateway = paymentGateway;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int bookingId)
        {
            if (bookingId <= 0) return BadRequest("Invalid Booking ID");
            // 1. إنشاء أو جلب الدفعة أونلاين بمبلغ الحجز الحقيقي من الداتابيز
            var payment = await _paymentService.CreateOrGetOnlinePaymentAsync(bookingId);
            // 2. تجهيز رابط العودة بعد انتهاء الدفع
            var returnUrl = Url.Action(nameof(Return), "CardPayment", null, Request.Scheme)
                            ?? $"/CardPayment/Return";
            // 3. إنشاء جلسة الدفع عبر البوابة
            var checkoutDto = await _paymentGateway.CreateCheckoutAsync(payment, returnUrl);
            // 4. حفظ الـ GatewayReference في الداتابيز أولاً
            await _paymentService.UpdateGatewayReferenceAsync(payment.Id, checkoutDto.GatewayReference);
            // 5. التوجيه لبوابة الدفع
            return Redirect(checkoutDto.CheckoutUrl);
        }

        [HttpGet]
        public async Task<IActionResult> FakeCheckout(string reference, string returnUrl)
        {
            var payment = await _paymentService.GetPaymentByReferenceAsync(reference);
            if (payment == null) return NotFound("Payment session not found.");

            ViewBag.Reference = reference;
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Amount = payment.Amount;
            ViewBag.BookingId = payment.BookingId;

            return View("~/Views/Payment/CardCheckout.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FakeProcess(string reference, string returnUrl, string cardNumber, string actionType)
        {
            var payment = await _paymentService.GetPaymentByReferenceAsync(reference);
            if (payment == null) return NotFound();
            if (actionType == "cancel")
            {
                return RedirectToAction(nameof(Return), new { reference = reference, success = false });
            }
            // استخراج بيانات كارت التيست
            var cleanCard = (cardNumber ?? "").Replace(" ", "");
            var cardLast4 = cleanCard.Length >= 4 ? cleanCard[^4..] : "4242";
            var cardBrand = cleanCard.StartsWith("4") ? "Visa" : "Mastercard";
            // اعتماد الدفع
            await _paymentService.AuthorizePaymentAsync(reference, cardBrand, cardLast4);
            return RedirectToAction(nameof(Return), new { reference = reference, success = true });
        }
        [HttpGet]
        public async Task<IActionResult> Return(string reference, bool success = true)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return RedirectToAction("Index", "Home");
            }

            var payment = await _paymentService.GetPaymentByReferenceAsync(reference);
            if (payment == null) return NotFound();

            ViewBag.IsSuccess = success && (payment.Status == PaymentStatus.Authorized || payment.Status == PaymentStatus.Captured);
            return View("~/Views/Payment/CardResult.cshtml", payment);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // Webhook سيرفر-لسيرفر من بوابة الدفع
        public async Task<IActionResult> Webhook()
        {
            var verificationResult = await _paymentGateway.VerifyWebhookAsync(Request);

            if (!verificationResult.IsSuccessful)
            {
                return BadRequest(new { error = verificationResult.ErrorMessage });
            }

            // التحقق والاعتماد Idempotent
            var authorized = await _paymentService.AuthorizePaymentAsync(
                verificationResult.GatewayReference,
                verificationResult.CardBrand ?? "Visa",
                verificationResult.CardLast4 ?? "4242"
            );

            if (!authorized)
            {
                // إذا لم توجد دفعة بهذا المرجع
                return NotFound(new { error = "Payment reference not found." });
            }

            return Ok(new { status = "success" });
        }
    }
}