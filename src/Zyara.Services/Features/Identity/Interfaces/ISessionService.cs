using System.Threading;
using System.Threading.Tasks;
using Zyara.Domain.Entities;

namespace Zyara.Services.Features.Identity.Interfaces;

/// <summary>
/// T9 (Adham): Session management, sliding cookie refresh, and security-stamp invalidation.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Refreshes the sign-in cookie for the specified user on the current device
    /// (e.g. after changing password, updating 2FA settings, or during keep-alive).
    /// </summary>
    Task RefreshAsync(User user);

    /// <summary>
    /// Invalidates all active sessions for the specified user on other devices
    /// by rotating their SecurityStamp in the database.
    /// When the SecurityStamp changes, SecurityStampValidator rejects existing cookies
    /// within its validation interval (configured to 1 minute).
    /// </summary>
    Task InvalidateAsync(int userId, CancellationToken cancellationToken = default);
}
