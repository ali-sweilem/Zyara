using Zyara.Domain.Entities;

namespace Zyara.Services.Features.Identity.Interfaces;

/// <summary>
/// T9 (Adham).
/// NOTE: PasswordRecoveryService finds this interface by name and calls InvalidateAsync(int) by reflection,
/// so keep the namespace, the method name and the (int) signature exactly as they are.
/// </summary>
public interface ISessionService
{
    /// <summary>Re-issues the cookie of the CURRENT device with fresh claims (after a password / 2FA change).</summary>
    Task RefreshAsync(User user);

    /// <summary>Changes the SecurityStamp: every other device is signed out within ~1 minute.</summary>
    Task InvalidateAsync(int userId);
}
