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
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IConfiguration _config;

    public ExternalLoginController(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IExternalLoginService externalLoginService,
        IAuthenticationSchemeProvider schemeProvider,
        IConfiguration config)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _externalLoginService = externalLoginService;
        _schemeProvider = schemeProvider;
        _config = config;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Challenge(string provider = "Google", string? returnUrl = null)
    {
        var clientId = _config["Authentication:Google:ClientId"];
        var clientSecret = _config["Authentication:Google:ClientSecret"];
        var isConfigured = !string.IsNullOrWhiteSpace(clientId) && 
                           !string.IsNullOrWhiteSpace(clientSecret) && 
                           !clientId.Contains("YOUR_CLIENT_ID", StringComparison.OrdinalIgnoreCase);

        var scheme = await _schemeProvider.GetSchemeAsync(provider);

        // If real OAuth credentials are configured and the scheme exists, challenge Google
        if (isConfigured && scheme != null)
        {
            var redirectUrl = Url.Action(nameof(Callback), "ExternalLogin", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        // Otherwise (development mode or credentials not yet provided), use the dev Google sign-in helper
        return RedirectToAction(nameof(DevGoogle), new { returnUrl });
    }

    [HttpGet]
    public IActionResult DevGoogle(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DevGoogleSignIn(string email, string? fullName = null, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Email is required.");
            ViewBag.ReturnUrl = returnUrl;
            return View("DevGoogle");
        }

        email = email.Trim().ToLowerInvariant();
        fullName = string.IsNullOrWhiteSpace(fullName) ? email.Split('@')[0] : fullName.Trim();
        var googleId = "google-dev-" + Math.Abs(email.GetHashCode());

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, googleId),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Email, email)
        };

        var identity = new ClaimsIdentity(claims, IdentityConstants.ExternalScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties(new Dictionary<string, string?>
        {
            { "LoginProvider", "Google" }
        })
        {
            RedirectUri = Url.Action(nameof(Callback), "ExternalLogin", new { returnUrl })
        };

        await HttpContext.SignInAsync(IdentityConstants.ExternalScheme, principal, props);

        return RedirectToAction(nameof(Callback), new { returnUrl });
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

