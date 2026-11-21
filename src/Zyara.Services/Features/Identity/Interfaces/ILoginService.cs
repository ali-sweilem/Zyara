using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity.Dtos;

namespace Zyara.Services.Features.Identity.Interfaces;

/// <summary>Outcome of a login attempt. The controller only needs to react to these values.</summary>
public enum LoginStatus
{
    Succeeded,
    InvalidCredentials,   // wrong email, wrong password, or a deactivated account (one generic message)
    RequiresTwoFactor,    // password is right, the user still has to pass the authenticator step
    LockedOut,            // too many failed attempts
    NotAllowed            // e.g. email not confirmed yet
}

/// <summary>
/// The result of <see cref="ILoginService.LoginAsync"/>.
/// Keeps "what happened" separate from "what the page shows".
/// </summary>
public sealed class LoginResult
{
    private LoginResult(LoginStatus status, UserRole? role, string? message)
    {
        Status = status;
        Role = role;
        Message = message;
    }

    public LoginStatus Status { get; }

    /// <summary>Only set when <see cref="Status"/> is <see cref="LoginStatus.Succeeded"/>.</summary>
    public UserRole? Role { get; }

    /// <summary>Optional message for the page (for example "confirm your email first").</summary>
    public string? Message { get; }

    public bool Succeeded => Status == LoginStatus.Succeeded;

    public static LoginResult Success(UserRole role) => new(LoginStatus.Succeeded, role, null);

    public static LoginResult InvalidCredentials() => new(LoginStatus.InvalidCredentials, null, null);

    public static LoginResult TwoFactorRequired() => new(LoginStatus.RequiresTwoFactor, null, null);

    public static LoginResult LockedOut() => new(LoginStatus.LockedOut, null, null);

    public static LoginResult NotAllowed(string message) => new(LoginStatus.NotAllowed, null, message);
}

public interface ILoginService
{
    /// <summary>
    /// Checks the email/password with ASP.NET Core Identity (lockout on failure) and says what to do next.
    /// Never tells the caller whether the email exists.
    /// </summary>
    Task<LoginResult> LoginAsync(LoginDto dto);
}
