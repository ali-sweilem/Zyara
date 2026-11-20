using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Web.Infrastructure;

namespace Zyara.Web.Controllers;

/// <summary>
/// T4: Login (GET/POST) + Logout (POST) + AccessDenied.
/// The cookie itself is created/removed by SignInManager, this controller only calls the service
/// and decides which page to show next.
/// </summary>
public class AccountController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly ICurrentUser _currentUser;
    private readonly ILoginService _loginService;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ICurrentUser currentUser)
    {
        _signInManager = signInManager;
        _currentUser = currentUser;

        // The shared ServiceCollectionExtensions.cs files are owned by Ali, so we build the
        // service here. TODO (Ali): add "services.AddScoped<ILoginService, LoginService>();"
        // to IdentityServiceCollectionExtensions and inject ILoginService instead.
        _loginService = new LoginService(userManager, signInManager);
    }

    // GET /Account/Login
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Already signed in? No reason to show the form again.
        if (_currentUser.IsAuthenticated)
        {
            return LocalRedirect(LoginRedirects.HomeFor(_currentUser.Role ?? UserRole.Patient));
        }

        return View(new LoginDto { ReturnUrl = returnUrl });
    }

    // POST /Account/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _loginService.LoginAsync(dto);

        switch (result.Status)
        {
            case LoginStatus.Succeeded:
                return RedirectAfterLogin(dto.ReturnUrl, result.Role);

            case LoginStatus.RequiresTwoFactor:
                // T10: the authenticator step. The pending 2FA cookie is already set by Identity.
                return RedirectToAction("Verify", "TwoFactor", new { returnUrl = dto.ReturnUrl });

            case LoginStatus.LockedOut:
                return RedirectToAction(nameof(Lockout));

            case LoginStatus.NotAllowed:
                ModelState.AddModelError(string.Empty, result.Message ?? "You cannot sign in right now.");
                return View(dto);

            default:
                // One generic message for every credential problem (wrong email, wrong password, deactivated).
                ModelState.AddModelError(string.Empty, LoginService.InvalidCredentialsMessage);
                return View(dto);
        }
    }

    // POST /Account/Logout  (POST + anti-forgery so a link/image cannot log the user out)
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();   // deletes the auth cookie
        return RedirectToAction("Index", "Landing");
    }

    // GET /Account/AccessDenied
    // Reached automatically when an authenticated user hits a page their role may not see.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // GET /Account/Lockout
    // Reached after too many failed password attempts (and from the external-login flow).
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lockout() => View();

    /// <summary>ReturnUrl wins only when it is local; otherwise the role's home page.</summary>
    private IActionResult RedirectAfterLogin(string? returnUrl, UserRole? role)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return LocalRedirect(LoginRedirects.HomeFor(role ?? UserRole.Patient));
    }
}
