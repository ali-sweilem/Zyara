using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity.Business_Logic;

public class PasswordRecoveryService : IPasswordRecoveryService
{
    private readonly UserManager<User> _userManager;
    private readonly IAppEmailSender _emailSender;
    private readonly ISessionService _sessions;

    public PasswordRecoveryService(
        UserManager<User> userManager,
        IAppEmailSender _emailSender,
        ISessionService sessions)
    {
        _userManager = userManager;
        this._emailSender = _emailSender;
        _sessions = sessions;
    }

    public async Task ForgotPasswordAsync(string email, string originSchemeAndHost)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var user = await _userManager.FindByEmailAsync(email);
        // Do not reveal user existence; also check isActive if applicable
        if (user == null || !user.IsActive)
        {
            return;
        }

        // Generate password reset token (valid for 30 mins via DataProtectionTokenProviderOptions configured in AddZyaraIdentity)
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var resetLink = $"{originSchemeAndHost.TrimEnd('/')}/Password/Reset?userId={user.Id}&code={encodedToken}";
        var body = ResetPasswordEmailTemplate.Generate(resetLink);

        await _emailSender.SendAsync(user.Email!, "Reset Your Password - Zyara", body);
    }

    public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidUser",
                Description = "Invalid user or expired reset token."
            });
        }

        string decodedToken;
        try
        {
            var tokenBytes = WebEncoders.Base64UrlDecode(dto.Code);
            decodedToken = Encoding.UTF8.GetString(tokenBytes);
        }
        catch
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidToken",
                Description = "Invalid or corrupted password reset token."
            });
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.Password);
        if (result.Succeeded)
        {
            // ResetPasswordAsync already changes the SecurityStamp; InvalidateAsync makes the intent explicit
            // and signs the other devices out. A failure here must not fail a reset that already succeeded.
            try
            {
                await _sessions.InvalidateAsync(user.Id);
            }
            catch
            {
                // The SecurityStamp change from ResetPasswordAsync already covers the cookies.
            }
        }

        return result;
    }
}

