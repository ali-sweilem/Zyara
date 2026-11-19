using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Controllers;

[AllowAnonymous]
public class ExternalLoginController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IExternalLoginService _externalLoginService;

    public ExternalLoginController(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IExternalLoginService externalLoginService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _externalLoginService = externalLoginService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Challenge(string provider = "Google", string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(Callback), "ExternalLogin", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    [HttpGet]
    public async Task<IActionResult> Callback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["ErrorMessage"] = "Error loading external login information.";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        // Try external login sign in
        var signInResult = await _externalLoginService.ExternalLoginSignInAsync(info);
        if (signInResult.Succeeded)
        {
            return LocalRedirect(returnUrl);
        }

        if (signInResult.RequiresTwoFactor)
        {
            return RedirectToAction("Verify", "TwoFactor", new { ReturnUrl = returnUrl });
        }

        if (signInResult.IsLockedOut)
        {
            return RedirectToAction("Lockout", "Account");
        }

        // User does not have a linked login. Check if an account already exists with this email.
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Email claim not received from external provider.";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        var existingUser = await _externalLoginService.FindByEmailAsync(email);
        if (existingUser != null)
        {
            // Security: If user exists, only link automatically if their email is confirmed.
            // Otherwise, demand local password login first to prevent Account Takeover.
            if (existingUser.EmailConfirmed)
            {
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }

            // Local user email not confirmed or cannot link directly: require local login first
            TempData["ErrorMessage"] = "An account with this email already exists. Please log in with your credentials first to link your Google account.";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        // New user: Only Patients can register via Google. Providers require credentials verification.
        var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var phone = info.Principal.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty;

        var completeDto = new ExternalLoginCompleteDto
        {
            Email = email,
            FullName = name,
            PhoneNumber = phone,
            ReturnUrl = returnUrl
        };

        return View("Complete", completeDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(ExternalLoginCompleteDto dto)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["ErrorMessage"] = "Error loading external login information. Please try again.";
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var (result, patient) = await _externalLoginService.CompletePatientRegistrationAsync(dto, info);
        if (result.Succeeded && patient != null)
        {
            await _signInManager.SignInAsync(patient, isPersistent: false);
            var returnUrl = Url.IsLocalUrl(dto.ReturnUrl) ? dto.ReturnUrl : Url.Content("~/");
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(dto);
    }
}

