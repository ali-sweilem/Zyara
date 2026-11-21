namespace Zyara.Services.Features.Identity.Dtos;

/// <summary>
/// Data needed by the setup/manage pages: the shared key, the otpauth:// URI and a ready-to-show QR image.
/// </summary>
public sealed class TwoFactorSetupDto
{
    /// <summary>Base32 secret, grouped in 4s so it can be typed into the app manually.</summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>The standard otpauth://totp/... URI encoded in the QR code.</summary>
    public string AuthenticatorUri { get; set; } = string.Empty;

    /// <summary>PNG as a data URL ("data:image/png;base64,..."), so the view needs no extra endpoint.</summary>
    public string QrCodeImage { get; set; } = string.Empty;

    public bool IsTwoFactorEnabled { get; set; }

    /// <summary>How many unused recovery codes are left.</summary>
    public int RecoveryCodesLeft { get; set; }
}
