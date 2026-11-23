using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Controllers;

/// <summary>Login · Logout · AccessDenied · Lockout (the paths set in AddZyaraIdentity cookie options).</summary>
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly ILoginService _login;
    private readonly ICurrentUser _currentUser;

    public AccountController(ILoginService login, ICurrentUser currentUser)
    {
        _login = login;
        _currentUser = currentUser;
    }

    // GET /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_currentUser.IsAuthenticated) return RedirectAfterLogin(returnUrl, _currentUser.Role);
        return View(new LoginDto { ReturnUrl = returnUrl });
    }

    // POST /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _login.LoginAsync(dto);
        if (result.Succeeded) return RedirectAfterLogin(dto.ReturnUrl, result.Role);

        switch (result.Status)
        {
            case LoginStatus.LockedOut:
                return RedirectToAction(nameof(Lockout));

            case LoginStatus.EmailNotConfirmed:
                ViewData["ShowResend"] = true;
                ModelState.AddModelError(string.Empty, "Please confirm your email before signing in.");
                break;

            case LoginStatus.RequiresTwoFactor:
                return RedirectToAction("Verify", "TwoFactor", new { returnUrl = dto.ReturnUrl, rememberMe = dto.RememberMe });   // T10 (Youssef)

            default:
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                break;
        }

        return View(dto);
    }

    // POST /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _login.LogoutAsync();
        return RedirectToAction("Index", "Landing");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    public IActionResult Lockout() => View();

    private IActionResult RedirectAfterLogin(string? returnUrl, UserRole? role)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);

        return role switch
        {
            UserRole.Admin => RedirectToAction("Index", "Dashboard", new { area = "Admin" }),
            UserRole.Doctor or UserRole.Nurse => RedirectToAction("Index", "Schedule", new { area = "Provider" }),
            _ => RedirectToAction("Index", "Landing", new { area = "" })
        };
    }
}
