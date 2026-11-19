using Zyara.Domain.Entities;

namespace Zyara.Services.Features.Identity.Interfaces;

public interface IEmailConfirmationService
{
    /// <summary>Generates a confirmation token and emails the user a /EmailConfirmation/Confirm link.</summary>
    Task SendAsync(User user);

    /// <summary>Confirms the email. Returns false for any bad / used / expired / tampered link (no details leaked).</summary>
    Task<bool> ConfirmAsync(string? userId, string? code);

    /// <summary>
    /// Re-sends the confirmation email. Always completes silently, whether the email exists or not,
    /// so the caller can show one fixed message (no user enumeration).
    /// </summary>
    Task ResendAsync(string? email);
}
