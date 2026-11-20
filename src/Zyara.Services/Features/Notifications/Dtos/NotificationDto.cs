using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Notifications.Dtos;

public sealed class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }   // UTC
}
