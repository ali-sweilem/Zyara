using System;
using Zyara.Domain.Entities;

namespace Zyara.Domain.Entities
{
    public class PasswordResetCode
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string CodeHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public int AttemptCount { get; set; } = 0;
        public bool IsUsed { get; set; } = false;
        public DateTime? ConsumedAt { get; set; }
    }
}
