using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Notifications.Dtos;

namespace Zyara.Services.Features.Notifications.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Queues a notification for <paramref name="userId"/>. It does NOT save: it joins the caller's unit of work,
    /// so the notification is committed (or rolled back) together with the booking change that caused it.
    /// <paramref name="link"/> must be a local path such as "/Provider/Bookings"; anything else is dropped.
    /// </summary>
    void Notify(int userId, NotificationType type, string title, string message, string? link = null);

    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationDto>> GetPageAsync(int userId, int page, int pageSize = 15, CancellationToken cancellationToken = default);

    /// <summary>Marks one of THIS user's notifications as read. Found = false when it isn't theirs / doesn't exist.</summary>
    Task<(bool Found, string? Link)> MarkReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default);
}
