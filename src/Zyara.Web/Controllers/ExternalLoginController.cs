using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Controllers;

/// <summary>T11: Google sign-in. Existing linked users sign in; existing confirmed users are linked; new users become Patients.</summary>
[AllowAnonymous]
public class ExternalLoginController : Controller
{
    private const string GoogleProvider = "Google";

    private readonly SignInManager<User> _signInManager;
    private readonly IExternalLoginService _externalLoginService;
    private readonly IAuthenticationSchemeProvider _schemes;

    public ExternalLoginController(
        SignInManager<User> signInManager,
        IExternalLoginService externalLoginService,
        IAuthenticationSchemeProvider schemes)
    {
        _signInManager = signInManager;
        _externalLoginService = externalLoginService;
        _schemes = schemes;
    }

    // POST /ExternalLogin/Challenge : sends the browser to Google
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Challenge(string provider = GoogleProvider, string? returnUrl = null)
    {
        returnUrl = LocalOrNull(returnUrl);

        // Only Google, and only when its keys are configured (otherwise ChallengeResult would throw).
        if (!string.Equals(provider, GoogleProvider, StringComparison.Ordinal)
            || await _schemes.GetSchemeAsync(GoogleProvider) is null)
        {
            TempData["ErrorMessage"] = "Google sign-in is not available right now.";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        var redirectUrl = Url.Action(nameof(Callback), "ExternalLogin", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleProvider, redirectUrl);
        return new ChallengeResult(GoogleProvider, properties);
    }

    // GET /ExternalLogin/Callback : Google sends the user back here (after /signin-google)
    [HttpGet]
    public async Task<IActionResult> Callback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl = LocalOrNull(returnUrl);

        if (remoteError != null)
        {
            // Fixed text on purpose: never echo a query-string value into the page.
            TempData["ErrorMessage"] = "Google sign-in was cancelled or failed. Please try again.";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["ErrorMessage"] = "Error loading external login information. Please try again.";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        // 1) Already linked -> sign in
        var signIn = await _externalLoginService.ExternalLoginSignInAsync(info);
        var handled = HandleSignIn(signIn, returnUrl);
        if (handled != null) return handled;

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] = "Google did not return an email address for this account.";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        // 2) A local account with this email exists -> link only if it is active and its email is confirmed
        var existingUser = await _externalLoginService.FindByEmailAsync(email);
        if (existingUser != null)
        {
            var linked = await _externalLoginService.LinkAndSignInAsync(existingUser, info);
            handled = HandleSignIn(linked, returnUrl);
            if (handled != null) return handled;

            TempData["ErrorMessage"] = existingUser.EmailConfirmed
                ? "We could not link your Google account. Please sign in with your email and password."
                : "An account with this email already exists but its email is not confirmed yet. Confirm it (or resend the confirmation email), then try Google again.";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        // 3) New user: only Patients can register through Google. Doctors / nurses use the Register page (credentials review).
        var completeDto = new ExternalLoginCompleteDto
        {
            Email = email,
            FullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            PhoneNumber = string.Empty,   // Google does not send a phone number
            ReturnUrl = returnUrl
        };

        return View("Complete", completeDto);
    }

    // POST /ExternalLogin/Complete : finish creating the Patient
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(ExternalLoginCompleteDto dto)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["ErrorMessage"] = "Your Google session expired. Please try again.";
            return RedirectToAction("Login", "Account");
        }

        // Never trust the email from the form (a readonly input can be edited): use the one Google verified.
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] = "Google did not return an email address for this account.";
            return RedirectToAction("Login", "Account");
        }

        dto.Email = email;
        dto.ReturnUrl = LocalOrNull(dto.ReturnUrl);
        ModelState.Remove(nameof(dto.Email));

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        if (await _externalLoginService.FindByEmailAsync(email) != null)
        {
            TempData["ErrorMessage"] = "An account with this email already exists. Please sign in.";
            return RedirectToAction("Login", "Account", new { returnUrl = dto.ReturnUrl });
        }

        var (result, patient) = await _externalLoginService.CompletePatientRegistrationAsync(dto, info);
        if (result.Succeeded && patient != null)
        {
            // Normal external sign-in (also clears the temporary external cookie).
            var signIn = await _externalLoginService.ExternalLoginSignInAsync(info);
            var handled = HandleSignIn(signIn, dto.ReturnUrl);
            if (handled != null) return handled;

            TempData["ErrorMessage"] = "Your account was created, but we could not sign you in. Please try again.";
            return RedirectToAction("Login", "Account", new { returnUrl = dto.ReturnUrl });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(dto);
    }

    /// <summary>Maps a sign-in result to a redirect. Returns null when the result is a plain failure (caller decides).</summary>
    private IActionResult? HandleSignIn(Microsoft.AspNetCore.Identity.SignInResult result, string? returnUrl)
    {
        if (result.Succeeded)
        {
            // Account/Login (GET) already redirects a signed-in user to the right place for the role
            // (Admin / Provider / Patient) or to the local returnUrl, so we reuse it instead of copying that logic.
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction("Verify", "TwoFactor", new { returnUrl });
        }

        if (result.IsLockedOut)
        {
            return RedirectToAction("Lockout", "Account");
        }

        if (result.IsNotAllowed)
        {
            TempData["ErrorMessage"] = "This account cannot sign in right now. If you have not confirmed your email yet, confirm it first.";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        return null;
    }

    // LocalRedirect throws on an external url, so only local urls are ever kept.
    private string? LocalOrNull(string? url) =>
        !string.IsNullOrEmpty(url) && Url.IsLocalUrl(url) ? url : null;
}
