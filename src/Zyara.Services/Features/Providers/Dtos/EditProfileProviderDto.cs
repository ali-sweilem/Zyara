using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Providers.Dtos
{
    public class EditProfileProviderDto
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Full Name is required")]
        [RegularExpression(@"^[a-zA-Z\s]+$",ErrorMessage ="Full Name must contain letters and spaces only")]
        [StringLength(30, ErrorMessage = "Full Name cannot exceed 30 characters")]
        [Display(Name = "Full Name")]
        public String FullName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Phone is required")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone number must contain numbers only")]
        [StringLength(11, ErrorMessage = "Phone cannot exceed 11 characters")]
        [Display(Name = "Phone Number")]
        public String Phone { get; set; } = string.Empty;
        [Required(ErrorMessage = "Geder Not Selected")]
        public Gender Gender { get; set; }
        [Required(ErrorMessage = "Specialty is required")]
        [StringLength(100, ErrorMessage = "Specialty cannot exceed 100 characters")]
        [Display(Name = "Specialty")]
        public String Specialty { get; set; } = string.Empty;
        [Required(ErrorMessage = "Experience is required")]
        public int ExperienceYears { get; set; }
        [Required(ErrorMessage = "Base City is required")]
        public String BaseCity { get; set; } = string.Empty;
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [DataType(DataType.EmailAddress,ErrorMessage ="Email must contain number , letter , @and ended with .com")]
        public string Email { get; set; } = string.Empty;
        public string? Bio  { get; set; }

        public String? PhotoUrl { get; set; }
        public IFormFile? PhotoFile { get; set; }

    }
}
