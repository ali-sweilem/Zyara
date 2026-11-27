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

            if (!string.IsNullOrEmpty(link) && Url.IsLocalUrl(link))
                return LocalRedirect(link);

            if (User.IsInRole("Doctor") || User.IsInRole("Nurse"))
                return RedirectToAction("Index", "Dashboard", new { area = "Provider" });
            if (User.IsInRole("Patient"))
                return RedirectToAction("Index", "Patient", new { area = "" });
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "AdminUsers", new { area = "Admin" });

            return RedirectToAction(nameof(Index));
        }

        // GET /Notifications/Summary : latest 5 notifications + unread count for the bell dropdown
        [HttpGet]
        public async Task<IActionResult> Summary(CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.RequireUserId();
            var unreadCount = await _notifications.GetUnreadCountAsync(userId, cancellationToken);
            var page = await _notifications.GetPageAsync(userId, 1, 5, cancellationToken);

            return Json(new
            {
                unreadCount,
                items = page.Items.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type.ToString(),
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt.ToLocalTime().ToString("dd MMM, hh:mm tt")
                })
            });
        }

        // POST /Notifications/MarkAllRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
        {
            await _notifications.MarkAllReadAsync(_currentUser.RequireUserId(), cancellationToken);
            if (string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { unreadCount = 0 });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
