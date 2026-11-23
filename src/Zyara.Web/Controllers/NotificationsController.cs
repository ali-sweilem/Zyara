using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Notifications.Interfaces;

namespace Zyara.Web.Controllers
{
    /// <summary>The signed-in user's notifications (patients and providers). The user id always comes from ICurrentUser.</summary>
    [Authorize]
    public sealed class NotificationsController : Controller
    {
        private readonly INotificationService _notifications;
        private readonly ICurrentUser _currentUser;

        public NotificationsController(INotificationService notifications, ICurrentUser currentUser)
        {
            _notifications = notifications;
            _currentUser = currentUser;
        }

        // GET /Notifications
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
        {
            var model = await _notifications.GetPageAsync(_currentUser.RequireUserId(), page, 15, cancellationToken);
            return View(model);
        }

        // POST /Notifications/Open/5 : marks it read, then goes to its page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Open(int id, CancellationToken cancellationToken)
        {
            var (found, link) = await _notifications.MarkReadAsync(_currentUser.RequireUserId(), id, cancellationToken);
            if (!found) return NotFound();

            return !string.IsNullOrEmpty(link) && Url.IsLocalUrl(link)
                ? LocalRedirect(link)
                : RedirectToAction(nameof(Index));
        }

        // POST /Notifications/MarkAllRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
        {
            await _notifications.MarkAllReadAsync(_currentUser.RequireUserId(), cancellationToken);
            return RedirectToAction(nameof(Index));
        }
    }
}
