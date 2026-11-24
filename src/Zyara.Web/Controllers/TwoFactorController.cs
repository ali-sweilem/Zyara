using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Controllers;

/// <summary>T10: authenticator-app two-factor. Verify / UseRecoveryCode are the second step of login; the rest need a signed-in user.</summary>
[Authorize]
public class TwoFactorController : Controller
{
    private readonly ITwoFactorService _twoFactor;
    private readonly ICurrentUser _currentUser;

    public TwoFactorController(ITwoFactorService twoFactor, ICurrentUser currentUser)
    {
        _twoFactor = twoFactor;
        _currentUser = currentUser;
    }

    // ===== Login step 2 (anonymous: the user only passed the password so far) =====

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Verify(string? returnUrl = null, bool rememberMe = false)
    {
        if (!await _twoFactor.HasPendingLoginAsync()) return RedirectToAction("Login", "Account");
        return View(new TwoFactorVerifyDto { ReturnUrl = returnUrl, RememberMe = rememberMe });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(TwoFactorVerifyDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _twoFactor.VerifyAsync(dto);
        if (result.Succeeded) return RedirectToAction("Login", "Account", new { returnUrl = dto.ReturnUrl });   // Login GET redirects by role

        if (result.Status == LoginStatus.LockedOut) return RedirectToAction("Lockout", "Account");

        ModelState.AddModelError(nameof(dto.Code), "Invalid code. Try again.");
        return View(dto);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> UseRecoveryCode(string? returnUrl = null)
    {
        if (!await _twoFactor.HasPendingLoginAsync()) return RedirectToAction("Login", "Account");
        return View(new RecoveryCodeLoginDto { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UseRecoveryCode(RecoveryCodeLoginDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _twoFactor.UseRecoveryCodeAsync(dto.RecoveryCode);
        if (result.Succeeded) return RedirectToAction("Login", "Account", new { returnUrl = dto.ReturnUrl });

        if (result.Status == LoginStatus.LockedOut) return RedirectToAction("Lockout", "Account");

        ModelState.AddModelError(nameof(dto.RecoveryCode), "Invalid recovery code.");
        return View(dto);
    }

    // ===== Manage (signed-in) =====

    [HttpGet]
    public async Task<IActionResult> Manage()
    {
        var status = await _twoFactor.GetStatusAsync(_currentUser.RequireUserId());
        return View(status);
    }

    [HttpGet]
    public async Task<IActionResult> Setup()
    {
        var userId = _currentUser.RequireUserId();
        if ((await _twoFactor.GetStatusAsync(userId)).IsEnabled) return RedirectToAction(nameof(Manage));

        return View(await _twoFactor.BeginSetupAsync(userId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup(TwoFactorSetupDto dto)
    {
        var userId = _currentUser.RequireUserId();

        if (ModelState.IsValid)
        {
            var codes = await _twoFactor.EnableAsync(userId, dto.Code);
            if (codes is not null) return View("RecoveryCodes", codes);   // shown once, not redirected, not stored

            ModelState.AddModelError(nameof(dto.Code), "That code is not valid. Check the time on your phone and try again.");
        }

        // Re-render the QR (the key is unchanged, so the user's existing scan still works).
        var fresh = await _twoFactor.BeginSetupAsync(userId);
        dto.SharedKey = fresh.SharedKey;
        dto.QrCodeBase64 = fresh.QrCodeBase64;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(DisableTwoFactorDto dto)
    {
        var ok = await _twoFactor.DisableAsync(_currentUser.RequireUserId(), dto.Password);
        TempData[ok ? "Message" : "Error"] = ok
            ? "Two-factor authentication is now off."
            : "Incorrect password. Two-factor authentication is still on.";
        return RedirectToAction(nameof(Manage));
    }
}
