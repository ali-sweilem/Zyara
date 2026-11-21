using Microsoft.AspNetCore.Identity;
using QRCoder;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

/// <summary>
/// T10: authenticator-app 2FA (TOTP). No SMS.
/// All the real work (secret, token check, recovery codes) is done by ASP.NET Core Identity;
/// this service only adds the QR code, the "current user" lookup and the session refresh.
/// </summary>
public sealed class TwoFactorService : ITwoFactorService
{
    private readonly UserManager<User> _users;
    private readonly SignInManager<User> _signIn;
    private readonly ICurrentUser _currentUser;

    public TwoFactorService(UserManager<User> users, SignInManager<User> signIn, ICurrentUser currentUser)
    {
        _users = users;
        _signIn = signIn;
        _currentUser = currentUser;
    }

    public async Task<TwoFactorSetupDto> BeginSetupAsync()
    {
        var user = await GetCurrentUserAsync();

        // One secret per user. Created on the first visit, reused afterwards (so a refresh shows the same QR).
        var key = await _users.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await _users.ResetAuthenticatorKeyAsync(user);
            key = await _users.GetAuthenticatorKeyAsync(user);
        }

        key ??= string.Empty;
        var email = user.Email ?? user.UserName ?? "user";

        // The standard URI that Google/Microsoft Authenticator understands.
        var uri = $"otpauth://totp/Zyara:{Uri.EscapeDataString(email)}?secret={key}&issuer=Zyara";

        return new TwoFactorSetupDto
        {
            SharedKey = GroupKey(key),
            AuthenticatorUri = uri,
            QrCodeImage = CreateQrCodeDataUrl(uri),
            IsTwoFactorEnabled = user.TwoFactorEnabled,
            RecoveryCodesLeft = await _users.CountRecoveryCodesAsync(user)
        };
    }

    public async Task<TwoFactorSetupDto> GetStatusAsync()
    {
        var user = await GetCurrentUserAsync();

        return new TwoFactorSetupDto
        {
            IsTwoFactorEnabled = user.TwoFactorEnabled,
            RecoveryCodesLeft = await _users.CountRecoveryCodesAsync(user)
        };
    }

    public async Task<(IdentityResult Result, IReadOnlyList<string> RecoveryCodes)> EnableAsync(string code)
    {
        var user = await GetCurrentUserAsync();

        // The code must match the secret at this moment before we switch 2FA on.
        var codeIsValid = await _users.VerifyTwoFactorTokenAsync(
            user,
            _users.Options.Tokens.AuthenticatorTokenProvider,
            NormalizeAuthenticatorCode(code));

        if (!codeIsValid)
        {
            return (IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidAuthenticatorCode",
                Description = "That code is not valid. Check your phone's clock and try the current code again."
            }), Array.Empty<string>());
        }

        var enable = await _users.SetTwoFactorEnabledAsync(user, true);
        if (!enable.Succeeded)
        {
            return (enable, Array.Empty<string>());
        }

        // 10 single-use recovery codes. Identity stores hashes; we show the plain codes once.
        var recoveryCodes = await _users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        await RefreshSessionAsync(user);

        return (IdentityResult.Success, recoveryCodes?.ToArray() ?? Array.Empty<string>());
    }

    public async Task<IdentityResult> DisableAsync(string password)
    {
        var user = await GetCurrentUserAsync();

        // Turning 2FA off is a security downgrade, so we ask for the password again.
        if (string.IsNullOrWhiteSpace(password) || !await _users.CheckPasswordAsync(user, password))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidPassword",
                Description = "Password is incorrect."
            });
        }

        var disable = await _users.SetTwoFactorEnabledAsync(user, false);
        if (!disable.Succeeded)
        {
            return disable;
        }

        // Forget the old secret: enabling again creates a new QR code.
        await _users.ResetAuthenticatorKeyAsync(user);
        await RefreshSessionAsync(user);

        return IdentityResult.Success;
    }

    public Task<SignInResult> VerifyCodeAsync(string code, bool rememberMe)
    {
        // rememberClient:false -> the browser is not remembered; the code is asked on every new login.
        return _signIn.TwoFactorAuthenticatorSignInAsync(
            NormalizeAuthenticatorCode(code),
            isPersistent: rememberMe,
            rememberClient: false);
    }

    public Task<SignInResult> VerifyRecoveryCodeAsync(string code)
    {
        return _signIn.TwoFactorRecoveryCodeSignInAsync((code ?? string.Empty).Trim());
    }

    public Task<User?> GetPendingUserAsync() => _signIn.GetTwoFactorAuthenticationUserAsync();

    // ---------- helpers ----------

    private async Task<User> GetCurrentUserAsync()
    {
        // Rule: the id comes from ICurrentUser, never from a form/query value.
        var id = _currentUser.RequireUserId();
        return await _users.FindByIdAsync(id.ToString())
               ?? throw new InvalidOperationException("The signed-in user no longer exists.");
    }

    private static string NormalizeAuthenticatorCode(string? code) =>
        (code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty).Trim();

    /// <summary>"JBSWY3DPEHPK3PXP" -> "JBSW Y3DP EHPK 3PXP" (easier to type by hand).</summary>
    private static string GroupKey(string key)
    {
        var parts = new List<string>();
        for (var i = 0; i < key.Length; i += 4)
        {
            parts.Add(key.Substring(i, Math.Min(4, key.Length - i)));
        }
        return string.Join(" ", parts);
    }

    /// <summary>Makes a PNG QR code and returns it as a data URL, ready for an &lt;img src="..."&gt;.</summary>
    private static string CreateQrCodeDataUrl(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(10);   // 10 px per module
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }

    private async Task RefreshSessionAsync(User user)
    {
        // Keep the current cookie in sync after the security change.
        await _signIn.RefreshSignInAsync(user);

        // TODO (T9 / Adham): ISessionService is not in the repo yet. When it lands, register it and call
        // "await _sessionService.RefreshAsync(user);" here (and in PasswordRecoveryService) instead.
    }
}
