using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Areas.Provider.Controllers
{
    /// <summary>
    /// The provider's bookings: accept / reject, start (OTP), complete.
    /// The provider id ALWAYS comes from ICurrentUser, never from a form or the URL.
    /// </summary>
    [Area("Provider")]
    [Authorize(Roles = "Doctor,Nurse")]
    public sealed class BookingsController : Controller
    {
        private readonly IProviderBookingService _bookings;
        private readonly IBookingCancellationService _cancellations;
        private readonly ICurrentUser _currentUser;

        public BookingsController(
            IProviderBookingService bookings,
            IBookingCancellationService cancellations,
            ICurrentUser currentUser)
        {
            _bookings = bookings;
            _cancellations = cancellations;
            _currentUser = currentUser;
        }

        // GET /Provider/Bookings : booking requests (what needs my action)
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var providerId = _currentUser.RequireUserId();
            var model = await _bookings.GetRequestsAsync(providerId, cancellationToken);
            model.CancellationStatus = await _cancellations.GetProviderStatusAsync(providerId, cancellationToken);
            return View(model);
        }

        // GET /Provider/Bookings/History?status=Completed&page=2 : every booking, any status
        [HttpGet]
        public async Task<IActionResult> History(BookingStatus? status, int page = 1, CancellationToken cancellationToken = default)
        {
            var model = await _bookings.GetHistoryAsync(_currentUser.RequireUserId(), status, page, 10, cancellationToken);
            ViewData["Status"] = status;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id, CancellationToken cancellationToken) =>
            Done(await _bookings.AcceptAsync(_currentUser.RequireUserId(), id, cancellationToken),
                 "Booking accepted.");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken) =>
            Done(await _bookings.RejectAsync(_currentUser.RequireUserId(), id, cancellationToken),
                 "Booking rejected.");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id, string? otp, CancellationToken cancellationToken) =>
            Done(await _bookings.StartAsync(_currentUser.RequireUserId(), id, otp, cancellationToken),
                 "Visit started.");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, CancellationToken cancellationToken) =>
            Done(await _bookings.CompleteAsync(_currentUser.RequireUserId(), id, cancellationToken),
                 "Booking completed.");

        // POST /Provider/Bookings/Cancel/5 : cancel a CONFIRMED booking (same-day cancellations cost money and count)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            var result = await _cancellations.CancelByProviderAsync(_currentUser.RequireUserId(), id, cancellationToken);

            if (result.Succeeded) TempData["Message"] = result.Value!.Message;
            else TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Index));
        }

        private IActionResult Done(ServiceResult result, string successMessage)
        {
            if (result.Succeeded) TempData["Message"] = successMessage;
            else TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Index));
        }
    }
}
