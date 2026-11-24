using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using QRCoder;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

public sealed class TwoFactorService : ITwoFactorService
{
    private const string Issuer = "Zyara";

    private readonly UserManager<User> _users;
    private readonly SignInManager<User> _signIn;
    private readonly ISessionService _sessions;

    public TwoFactorService(UserManager<User> users, SignInManager<User> signIn, ISessionService sessions)
    {
        _users = users;
        _signIn = signIn;
        _sessions = sessions;
    }

    public async Task<TwoFactorStatusDto> GetStatusAsync(int userId)
    {
        var user = await RequireUserAsync(userId);
        var enabled = await _users.GetTwoFactorEnabledAsync(user);
        var left = enabled ? await _users.CountRecoveryCodesAsync(user) : 0;
        return new TwoFactorStatusDto(enabled, left, await _users.HasPasswordAsync(user));
    }

    public async Task<TwoFactorSetupDto> BeginSetupAsync(int userId)
    {
        var user = await RequireUserAsync(userId);

        var key = await _users.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _users.ResetAuthenticatorKeyAsync(user);
            key = await _users.GetAuthenticatorKeyAsync(user);
        }

        var uri = BuildOtpAuthUri(user.Email ?? user.UserName ?? userId.ToString(), key!);
        return new TwoFactorSetupDto
        {
            SharedKey = FormatKey(key!),
            QrCodeBase64 = RenderQr(uri)
        };
    }

    public async Task<RecoveryCodesDto?> EnableAsync(int userId, string code)
    {
        var user = await RequireUserAsync(userId);

        var valid = await _users.VerifyTwoFactorTokenAsync(
            user, _users.Options.Tokens.AuthenticatorTokenProvider, Clean(code));
        if (!valid) return null;

        await _users.SetTwoFactorEnabledAsync(user, true);
        var codes = await _users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        await _sessions.RefreshAsync(user);
        return new RecoveryCodesDto((codes ?? Enumerable.Empty<string>()).ToList());
    }

    public async Task<bool> DisableAsync(int userId, string? password)
    {
        var user = await RequireUserAsync(userId);

        // Accounts created with Google have no password: they are already authenticated by the cookie.
        if (await _users.HasPasswordAsync(user))
        {
            if (string.IsNullOrEmpty(password) || !await _users.CheckPasswordAsync(user, password))
                return false;
        }

        await _users.SetTwoFactorEnabledAsync(user, false);
        await _users.ResetAuthenticatorKeyAsync(user);   // a new setup later gets a brand new secret

        await _sessions.RefreshAsync(user);
        return true;
    }

    public async Task<bool> HasPendingLoginAsync() =>
        await _signIn.GetTwoFactorAuthenticationUserAsync() is not null;

    public async Task<LoginResultDto> VerifyAsync(TwoFactorVerifyDto dto)
    {
        var user = await _signIn.GetTwoFactorAuthenticationUserAsync();
        if (user is null || !user.IsActive) return LoginResultDto.Failed(LoginStatus.InvalidCredentials);

        var result = await _signIn.TwoFactorAuthenticatorSignInAsync(
            Clean(dto.Code), dto.RememberMe, dto.RememberMachine);

        return ToResult(result, user);
    }

    public async Task<LoginResultDto> UseRecoveryCodeAsync(string recoveryCode)
    {
        var user = await _signIn.GetTwoFactorAuthenticationUserAsync();
        if (user is null || !user.IsActive) return LoginResultDto.Failed(LoginStatus.InvalidCredentials);

        var result = await _signIn.TwoFactorRecoveryCodeSignInAsync(Clean(recoveryCode));
        return ToResult(result, user);
    }

    // ----- helpers -----

    private static LoginResultDto ToResult(SignInResult result, User user)
    {
        if (result.Succeeded) return new LoginResultDto(LoginStatus.Succeeded, user.Role);
        if (result.IsLockedOut) return LoginResultDto.Failed(LoginStatus.LockedOut);
        return LoginResultDto.Failed(LoginStatus.InvalidCredentials);
    }

    private async Task<User> RequireUserAsync(int userId) =>
        await _users.FindByIdAsync(userId.ToString())
        ?? throw new InvalidOperationException("User not found.");

    /// <summary>Users type codes as "123 456" or "ABCDE-12345"; Identity wants them without separators.</summary>
    private static string Clean(string code) => (code ?? string.Empty).Replace(" ", "").Replace("-", "");

    private static string BuildOtpAuthUri(string email, string key)
    {
        var enc = UrlEncoder.Default;
        return $"otpauth://totp/{enc.Encode(Issuer)}:{enc.Encode(email)}?secret={key}&issuer={enc.Encode(Issuer)}&digits=6";
    }

    private static string FormatKey(string key)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < key.Length; i += 4)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(key.AsSpan(i, Math.Min(4, key.Length - i)));
        }
        return sb.ToString().ToLowerInvariant();
    }

    private static string RenderQr(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(5);
        return Convert.ToBase64String(png);
    }
}
