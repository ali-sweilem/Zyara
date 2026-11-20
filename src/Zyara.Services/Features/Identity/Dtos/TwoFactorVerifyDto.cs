using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Identity.Dtos;

/// <summary>
/// The authenticator code shown on the login step 2 / setup page.
/// Reused by Enable (setup) and Verify (login) because both only need a code.
/// </summary>
public sealed class TwoFactorVerifyDto
{
    [Required(ErrorMessage = "Enter the code from your authenticator app.")]
    [StringLength(20, ErrorMessage = "That code is too long.")]
    [Display(Name = "Authenticator code")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Remember this browser")]
    public bool RememberMe { get; set; }

    /// <summary>Where to go after a successful verification (only used when local).</summary>
    public string? ReturnUrl { get; set; }
}
