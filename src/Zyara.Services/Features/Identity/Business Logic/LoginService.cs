using Microsoft.AspNetCore.Identity;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

public sealed class LoginService : ILoginService
{
    private readonly UserManager<User> _users;
    private readonly SignInManager<User> _signIn;

    public LoginService(UserManager<User> users, SignInManager<User> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email.Trim());

        // Unknown email and deactivated account get the same generic failure (SRS 3.3.2, no user enumeration).
        if (user is null || !user.IsActive)
        {
            return LoginResultDto.Failed(LoginStatus.InvalidCredentials);
        }

        // 5 failed attempts -> locked 10 minutes (AddZyaraIdentity). Handles 2FA, so it can never be skipped.
        var result = await _signIn.PasswordSignInAsync(user, dto.Password, dto.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded) return new LoginResultDto(LoginStatus.Succeeded, user.Role);
        if (result.RequiresTwoFactor) return LoginResultDto.Failed(LoginStatus.RequiresTwoFactor);
        if (result.IsLockedOut) return LoginResultDto.Failed(LoginStatus.LockedOut);
        if (result.IsNotAllowed) return LoginResultDto.Failed(LoginStatus.EmailNotConfirmed);

        return LoginResultDto.Failed(LoginStatus.InvalidCredentials);
    }

    public Task LogoutAsync() => _signIn.SignOutAsync();
}
