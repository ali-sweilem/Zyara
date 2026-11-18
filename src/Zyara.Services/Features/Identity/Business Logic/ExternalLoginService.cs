using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity.Business_Logic;

public class ExternalLoginService : IExternalLoginService
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public ExternalLoginService(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<SignInResult> ExternalLoginSignInAsync(ExternalLoginInfo info)
    {
        return await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: false);
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<(IdentityResult Result, User? User)> CompletePatientRegistrationAsync(
        ExternalLoginCompleteDto dto,
        ExternalLoginInfo info)
    {
        // Only Patients can be created via Google login
        var patient = new Patient
        {
            UserName = dto.Email,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            FullName = dto.FullName,
            Role = UserRole.Patient,
            IsActive = true,
            EmailConfirmed = true, // External email verified by Google
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(patient);
        if (!createResult.Succeeded)
        {
            return (createResult, null);
        }

        var roleResult = await _userManager.AddToRoleAsync(patient, nameof(UserRole.Patient));
        if (!roleResult.Succeeded)
        {
            return (roleResult, patient);
        }

        var addLoginResult = await _userManager.AddLoginAsync(patient, info);
        if (!addLoginResult.Succeeded)
        {
            return (addLoginResult, patient);
        }

        return (IdentityResult.Success, patient);
    }
}

