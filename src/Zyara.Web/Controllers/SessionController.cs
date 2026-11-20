using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Controllers;

/// <summary>
/// T9 (Adham): Keep-alive and sliding cookie refresh endpoint.
/// Hit periodically by <c>session-keepalive.js</c>.
/// Only authenticated users can call it.
/// </summary>
[Authorize]
[ApiController]
[Route("[controller]")]
public sealed class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public SessionController(
        ISessionService sessionService,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        _sessionService = sessionService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    /// <summary>
    /// POST /Session/Refresh
    /// Refreshes the auth cookie for sliding expiration.
    /// Returns 204 on success, 401 if not logged in or deactivated.
    /// </summary>
    [HttpPost("Refresh")]
    [HttpPost("Ping")] // Alias for backward compatibility
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refresh()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null || !user.IsActive)
        {
            await _signInManager.SignOutAsync();
            return Unauthorized();
        }

        await _sessionService.RefreshAsync(user);
        return NoContent(); // 204
    }
}
