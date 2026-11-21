using Microsoft.AspNetCore.Identity;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

/// <summary>
/// T4: email + password login using ASP.NET Core Identity cookie authentication.
/// The controller does not know anything about Identity; it just reacts to <see cref="LoginResult"/>.
/// </summary>
public sealed class LoginService : ILoginService
{
    /// <summary>The only message shown for a failed email/password login (never says which part was wrong).</summary>
    public const string InvalidCredentialsMessage = "Email or password is incorrect.";

    private readonly UserManager<User> _users;
    private readonly SignInManager<User> _signIn;

    public LoginService(UserManager<User> users, SignInManager<User> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    public async Task<LoginResult> LoginAsync(LoginDto dto)
    {
        var email = dto.Email?.Trim() ?? string.Empty;
        if (email.Length == 0 || string.IsNullOrWhiteSpace(dto.Password))
        {
            return LoginResult.InvalidCredentials();
        }

        var user = await _users.FindByEmailAsync(email);

        // Same result for "no such email" and "deactivated account": the page must not reveal
        // whether an email is registered, and a deactivated user must be treated as a stranger.
        if (user is null || !user.IsActive)
        {
            return LoginResult.InvalidCredentials();
        }

        // Identity checks the password, counts failed attempts (lockout after 5) and creates the auth cookie.
        var result = await _signIn.PasswordSignInAsync(
            user,
            dto.Password,
            isPersistent: dto.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LoginResult.Success(user.Role);
        }

        if (result.RequiresTwoFactor)
        {
            // T10: password is right, but the user still has to enter an authenticator code.
            return LoginResult.TwoFactorRequired();
        }

        if (result.IsLockedOut)
        {
            return LoginResult.LockedOut();
        }

        if (result.IsNotAllowed)
        {
            // Usually: RequireConfirmedEmail is on and this user has not confirmed yet (T7).
            return LoginResult.NotAllowed(
                "Please confirm your email address before signing in. Check your inbox for the confirmation link.");
        }

        return LoginResult.InvalidCredentials();
    }
}
