using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

/// <summary>
/// T9 (Adham): Session management implementation.
/// Handles cookie re-signing (<see cref="RefreshAsync"/>) and cross-device session termination (<see cref="InvalidateAsync"/>).
/// </summary>
public sealed class SessionService : ISessionService
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public SessionService(
        SignInManager<User> signInManager,
        UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <inheritdoc/>
    public async Task RefreshAsync(User user)
    {
        if (user is null)
        {
            return;
        }

        await _signInManager.RefreshSignInAsync(user);
    }

    /// <inheritdoc/>
    public async Task InvalidateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        // Updating the SecurityStamp in the database causes all active cookies
        // for this user to be invalidated at the next SecurityStampValidator check (1 minute).
        await _userManager.UpdateSecurityStampAsync(user);
    }
}
