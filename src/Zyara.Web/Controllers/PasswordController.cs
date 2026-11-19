using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Controllers;

[AllowAnonymous]
public class PasswordController : Controller
{
    private readonly IPasswordRecoveryService _passwordRecoveryService;

    public PasswordController(IPasswordRecoveryService passwordRecoveryService)
    {
        _passwordRecoveryService = passwordRecoveryService;
    }

    [HttpGet]
    public IActionResult Forgot()
    {
        return View(new ForgotPasswordDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Forgot(ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var originSchemeAndHost = $"{Request.Scheme}://{Request.Host}";
        await _passwordRecoveryService.ForgotPasswordAsync(dto.Email, originSchemeAndHost);

        // Always show the confirmation page regardless of whether the email exists or not (Prevent user enumeration)
        return RedirectToAction(nameof(ForgotConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Reset(int userId, string code)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(code))
        {
            return RedirectToAction(nameof(Forgot));
        }

        var model = new ResetPasswordDto
        {
            UserId = userId,
            Code = code
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _passwordRecoveryService.ResetPasswordAsync(dto);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(ResetConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(dto);
    }

    [HttpGet]
    public IActionResult ResetConfirmation()
    {
        return View();
    }
}

