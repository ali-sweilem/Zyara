using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Identity.Dtos;

/// <summary>Data for the "set up the authenticator app" page, plus the 6-digit code the user types back.</summary>
public sealed class TwoFactorSetupDto
{
    /// <summary>The secret, formatted in groups of 4 for manual entry.</summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>PNG of the QR code, Base64 (render as data:image/png;base64,...).</summary>
    public string QrCodeBase64 { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the 6-digit code from your app.")]
    [Display(Name = "Verification code")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>State shown on the Manage page.</summary>
public sealed record TwoFactorStatusDto(bool IsEnabled, int RecoveryCodesLeft, bool HasPassword);

/// <summary>Disable form.</summary>
public sealed class DisableTwoFactorDto
{
    [DataType(DataType.Password)]
    public string? Password { get; set; }
}
