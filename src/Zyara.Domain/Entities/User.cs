using System;
using Microsoft.AspNetCore.Identity;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Entities
{
    public abstract class User : IdentityUser<int>
    {
        public string FullName { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public String? PhotoUrl { get; set; }
    }
}


