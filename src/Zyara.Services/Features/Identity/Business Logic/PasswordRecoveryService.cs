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
    private readonly IServiceProvider _serviceProvider;

    public PasswordRecoveryService(
        UserManager<User> userManager,
        IAppEmailSender _emailSender,
        IServiceProvider serviceProvider)
    {
        _userManager = userManager;
        this._emailSender = _emailSender;
        _serviceProvider = serviceProvider;
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
            // ResetPasswordAsync inherently changes the SecurityStamp, which invalidates active sessions.
            // In addition, if ISessionService is registered by Adham (T9), invoke InvalidateAsync(user.Id).
            try
            {
                var sessionServiceType = Type.GetType("Zyara.Services.Features.Identity.Interfaces.ISessionService, Zyara.Services");
                if (sessionServiceType != null)
                {
                    var sessionService = _serviceProvider.GetService(sessionServiceType);
                    if (sessionService != null)
                    {
                        var invalidateMethod = sessionServiceType.GetMethod("InvalidateAsync", new[] { typeof(int) });
                        if (invalidateMethod != null)
                        {
                            var task = invalidateMethod.Invoke(sessionService, new object[] { user.Id }) as Task;
                            if (task != null)
                            {
                                await task;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback: SecurityStamp change already handles cookie invalidation
            }
        }

        return result;
    }
}

