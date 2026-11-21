using Microsoft.AspNetCore.Identity;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Dtos;

namespace Zyara.Services.Features.Identity.Interfaces;

/// <summary>
/// T10: authenticator-app (TOTP) two-factor authentication, using ASP.NET Core Identity.
/// The current user always comes from <see cref="ICurrentUser"/> (never from a form or URL).
/// </summary>
public interface ITwoFactorService
{
    /// <summary>Gets (or creates) the authenticator key and builds the QR code for the setup page.</summary>
    Task<TwoFactorSetupDto> BeginSetupAsync();

    /// <summary>Enabled/disabled state + remaining recovery codes for the manage page.</summary>
    Task<TwoFactorSetupDto> GetStatusAsync();

    /// <summary>Checks the first code, enables 2FA and returns 10 fresh recovery codes (shown once).</summary>
    Task<(IdentityResult Result, IReadOnlyList<string> RecoveryCodes)> EnableAsync(string code);

    /// <summary>Turns 2FA off. The password is required, as agreed in T10.</summary>
    Task<IdentityResult> DisableAsync(string password);

    /// <summary>Login step 2 with a 6-digit authenticator code.</summary>
    Task<SignInResult> VerifyCodeAsync(string code, bool rememberMe);

    /// <summary>Login step 2 with a recovery code (each code works once).</summary>
    Task<SignInResult> VerifyRecoveryCodeAsync(string code);

    /// <summary>The user waiting in the 2FA step (from Identity's temporary 2FA cookie), or null.</summary>
    Task<User?> GetPendingUserAsync();
}
