using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Identity.Dtos;

public class RegisterProviderDto : RegisterBaseDto
{
    /// <summary>Only Doctor or Nurse are accepted (re-checked on the server; Admin is never allowed).</summary>
    [Required(ErrorMessage = "Choose Doctor or Nurse.")]
    [Display(Name = "I am a")]
    public UserRole? ProviderType { get; set; }

    [Required(ErrorMessage = "Specialty is required.")]
    [StringLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [Range(0, 60, ErrorMessage = "Years of experience must be between 0 and 60.")]
    [Display(Name = "Years of experience")]
    public int ExperienceYears { get; set; }

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    [Display(Name = "City")]
    public string BaseCity { get; set; } = string.Empty;

    /// <summary>Required for doctors (stored in Doctor.licenser). Nurses have no license field.</summary>
    [StringLength(100)]
    [Display(Name = "License number")]
    public string? LicenseNumber { get; set; }
}
