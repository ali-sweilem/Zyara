using Zyara.Services.Features.Identity.Dtos;

namespace Zyara.Services.Features.Identity.Interfaces;

/// <summary>T10: Two-factor authentication with an authenticator app (TOTP, no SMS).</summary>
public interface ITwoFactorService
{
    Task<TwoFactorStatusDto> GetStatusAsync(int userId);

    /// <summary>Creates the key if needed and returns the QR code + manual key.</summary>
    Task<TwoFactorSetupDto> BeginSetupAsync(int userId);

    /// <summary>Checks the code; on success turns 2FA on and returns 10 recovery codes (null = wrong code).</summary>
    Task<RecoveryCodesDto?> EnableAsync(int userId, string code);

    /// <summary>Turns 2FA off. Users that have a password must confirm it.</summary>
    Task<bool> DisableAsync(int userId, string? password);

    /// <summary>True when a password/Google login is waiting for its second step.</summary>
    Task<bool> HasPendingLoginAsync();

    /// <summary>Login step 2 with an authenticator code.</summary>
    Task<LoginResultDto> VerifyAsync(TwoFactorVerifyDto dto);

    /// <summary>Login step 2 with a (single-use) recovery code.</summary>
    Task<LoginResultDto> UseRecoveryCodeAsync(string recoveryCode);
}
