using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Identity.Dtos;

/// <summary>Step 2 of login: the 6-digit code from the authenticator app.</summary>
public sealed class TwoFactorVerifyDto
{
    [Required(ErrorMessage = "Enter the 6-digit code from your app.")]
    [Display(Name = "Authenticator code")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Don't ask again on this device")]
    public bool RememberMachine { get; set; }

    /// <summary>Carried from the login form ("remember me").</summary>
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

/// <summary>Step 2 of login when the user lost the phone: one recovery code.</summary>
public sealed class RecoveryCodeLoginDto
{
    [Required(ErrorMessage = "Enter one of your recovery codes.")]
    [Display(Name = "Recovery code")]
    public string RecoveryCode { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
