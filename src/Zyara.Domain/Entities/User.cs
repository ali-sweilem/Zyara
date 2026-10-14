using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Entities
{
    internal abstract class User
    {
        public int Id { get; set; }
        public int FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }


    }
}
