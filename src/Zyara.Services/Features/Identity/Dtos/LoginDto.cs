using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Identity.Dtos;

/// <summary>
/// What the login form posts. The password never leaves this DTO (never logged, never stored).
/// </summary>
public sealed class LoginDto
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    /// <summary>
    /// Page to go back to after a successful login. Only used when it is a local URL
    /// (checked with Url.IsLocalUrl) so it cannot become an open redirect.
    /// </summary>
    public string? ReturnUrl { get; set; }
}
