using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Identity.Dtos;

public class ExternalLoginCompleteDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

