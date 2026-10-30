using Zyara.Domain.Enums;

namespace Zyara.Web.Models
{
    public class RegisterViewModel
    {
        public UserRole Role { get; set; }
        public string FullName { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public DateOnly? DateOfBirth { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public bool TermsAccepted { get; set; }
    }
}
