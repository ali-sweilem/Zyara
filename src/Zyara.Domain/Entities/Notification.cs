using System;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public NotificationType Type { get; set; } = NotificationType.General;

        /// <summary>Local page to open when the notification is clicked (always starts with "/").</summary>
        public string? Link { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
