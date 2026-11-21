using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Web.Infrastructure;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Zyara.Web.Controllers;

/// <summary>
/// T10: authenticator-app two-factor authentication.
/// Setup / Manage / Enable / Disable need a signed-in user ([Authorize]).
/// Verify / UseRecoveryCode are anonymous on purpose: at that moment the user only has
/// Identity's temporary 2FA cookie, not a full auth cookie yet (also used after Google login).
/// </summary>
[Authorize]
public class TwoFactorController : Controller
{
    private const string RecoveryCodesTempDataKey = "RecoveryCodes";

    private readonly ITwoFactorService _twoFactor;
    private readonly ICurrentUser _currentUser;

    public TwoFactorController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ICurrentUser currentUser)
    {
        _currentUser = currentUser;

        // ServiceCollectionExtensions.cs is shared/owned by Ali, so the service is built here.
        // TODO (Ali): register "services.AddScoped<ITwoFactorService, TwoFactorService>();" and inject it.
        _twoFactor = new TwoFactorService(userManager, signInManager, currentUser);
    }

    // ---------- login step 2 (anonymous) ----------

    // GET /TwoFactor/Verify
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string? returnUrl = null)
    {
        var user = await _twoFactor.GetPendingUserAsync();
        if (user is null)
        {
            // Not in the middle of a login: nothing to verify.
            return _currentUser.IsAuthenticated
                ? LocalRedirect(LoginRedirects.HomeFor(_currentUser.Role ?? UserRole.Patient))
                : RedirectToAction("Login", "Account", new { returnUrl });
        }

        return View(new TwoFactorVerifyDto { ReturnUrl = returnUrl });
    }

    // POST /TwoFactor/Verify
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(TwoFactorVerifyDto dto)
    {
        var user = await _twoFactor.GetPendingUserAsync();
        if (user is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = dto.ReturnUrl });
        }

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _twoFactor.VerifyCodeAsync(dto.Code, dto.RememberMe);
        return AfterVerify(result, dto, user, "Invalid code. Check your authenticator app and try the current code.");
    }

    // GET /TwoFactor/UseRecoveryCode
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> UseRecoveryCode(string? returnUrl = null)
    {
        var user = await _twoFactor.GetPendingUserAsync();
        if (user is null)
        {
            return _currentUser.IsAuthenticated
                ? LocalRedirect(LoginRedirects.HomeFor(_currentUser.Role ?? UserRole.Patient))
                : RedirectToAction("Login", "Account", new { returnUrl });
        }

        return View(new TwoFactorVerifyDto { ReturnUrl = returnUrl });
    }

    // POST /TwoFactor/UseRecoveryCode
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UseRecoveryCode(TwoFactorVerifyDto dto)
    {
        var user = await _twoFactor.GetPendingUserAsync();
        if (user is null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = dto.ReturnUrl });
        }

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _twoFactor.VerifyRecoveryCodeAsync(dto.Code);
        return AfterVerify(result, dto, user, "Invalid or already used recovery code.");
    }

    // ---------- setup / manage (signed in) ----------

    // GET /TwoFactor/Setup
    [HttpGet]
    public async Task<IActionResult> Setup()
    {
        var model = await _twoFactor.BeginSetupAsync();
        if (model.IsTwoFactorEnabled)
        {
            return RedirectToAction(nameof(Manage));
        }

        return View(model);
    }

    // POST /TwoFactor/Enable
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(TwoFactorVerifyDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View("Setup", await _twoFactor.BeginSetupAsync());
        }

        var (result, recoveryCodes) = await _twoFactor.EnableAsync(dto.Code);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Same key/QR as before, so the user can retry without re-scanning.
            return View("Setup", await _twoFactor.BeginSetupAsync());
        }

        // Shown once on the next page; identity only keeps hashes of these codes.
        TempData[RecoveryCodesTempDataKey] = recoveryCodes.ToArray();
        return RedirectToAction(nameof(RecoveryCodes));
    }

    // GET /TwoFactor/RecoveryCodes  (one-time display)
    [HttpGet]
    public IActionResult RecoveryCodes()
    {
        var codes = TempData[RecoveryCodesTempDataKey] as string[];

        // Read once and remove, so a refresh cannot show them again.
        TempData.Remove(RecoveryCodesTempDataKey);

        if (codes is null || codes.Length == 0)
        {
            return RedirectToAction(nameof(Manage));
        }

        return View(new RecoveryCodesDto { Codes = codes });
    }

    // GET /TwoFactor/Manage
    [HttpGet]
    public async Task<IActionResult> Manage()
    {
        return View(await _twoFactor.GetStatusAsync());
    }

    // POST /TwoFactor/Disable  (password required)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(string password)
    {
        var result = await _twoFactor.DisableAsync(password);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Manage));
        }

        TempData["StatusMessage"] = "Two-factor authentication is disabled. You can sign in with just your password again.";
        return RedirectToAction(nameof(Manage));
    }

    // ---------- helpers ----------

    /// <summary>
    /// Turns the Identity sign-in result into the right page, and re-shows the form with a friendly error otherwise.
    /// </summary>
    private IActionResult AfterVerify(IdentitySignInResult result, TwoFactorVerifyDto dto, User user, string errorMessage)
    {
        if (result.Succeeded)
        {
            return RedirectAfterLogin(dto.ReturnUrl, user.Role);
        }

        if (result.IsLockedOut)
        {
            return RedirectToAction("Lockout", "Account");
        }

        ModelState.AddModelError(string.Empty, errorMessage);
        return View(dto);
    }

    /// <summary>ReturnUrl only when it is local (never an open redirect), otherwise the role's home page.</summary>
    private IActionResult RedirectAfterLogin(string? returnUrl, UserRole role)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return LocalRedirect(LoginRedirects.HomeFor(role));
    }
}
