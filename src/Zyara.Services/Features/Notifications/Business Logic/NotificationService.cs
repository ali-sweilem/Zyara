using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Notifications.Dtos;
using Zyara.Services.Features.Notifications.Interfaces;

namespace Zyara.Services.Features.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public NotificationService(IRepository<Notification> notifications, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public void Notify(int userId, NotificationType type, string title, string message, string? link = null)
    {
        _notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = Truncate(title, 150),
            Message = Truncate(message, 1000),
            Link = SafeLink(link),
            IsRead = false,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        });
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default) =>
        _notifications.Query()
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public Task<PagedResult<NotificationDto>> GetPageAsync(int userId, int page, int pageSize = 15, CancellationToken cancellationToken = default) =>
        _notifications.Query()
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .ToPagedAsync(
                n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    Link = n.Link,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                },
                page, pageSize, maxPageSize: 50, cancellationToken: cancellationToken);

    public async Task<(bool Found, string? Link)> MarkReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
    {
        // Scoped to the current user: someone else's notification id behaves like "not found".
        var notification = await _notifications.FirstOrDefaultAsync(
            n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (notification is null) return (false, null);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return (true, notification.Link);
    }

    public Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default) =>
        _notifications.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

    // ───────────────────────── helpers ─────────────────────────

    /// <summary>Only local paths are allowed ("/x"), never "//host" or "http://...": no open redirects.</summary>
    private static string? SafeLink(string? link)
    {
        if (string.IsNullOrWhiteSpace(link)) return null;

        link = link.Trim();
        if (!link.StartsWith('/') || link.StartsWith("//") || link.StartsWith("/\\")) return null;

        return Truncate(link, 300);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
