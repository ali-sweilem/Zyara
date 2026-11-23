using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Identity.Dtos;

public class LoginDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    /// <summary>Only used if it is a local URL (checked in the controller).</summary>
    public string? ReturnUrl { get; set; }
}
