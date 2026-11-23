using Microsoft.AspNetCore.Identity;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

public sealed class SessionService : ISessionService
{
    private readonly UserManager<User> _users;
    private readonly SignInManager<User> _signIn;

    public SessionService(UserManager<User> users, SignInManager<User> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    public Task RefreshAsync(User user) => _signIn.RefreshSignInAsync(user);

    public async Task InvalidateAsync(int userId)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null) return;

        var result = await _users.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not invalidate sessions: {string.Join("; ", result.Errors.Select(e => e.Code))}");
        }
    }
}
