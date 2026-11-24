using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity.Business_Logic;

public class ExternalLoginService : IExternalLoginService
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ExternalLoginService> _logger;

    public ExternalLoginService(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        ILogger<ExternalLoginService> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<SignInResult> ExternalLoginSignInAsync(ExternalLoginInfo info)
    {
        // A deactivated account must not get in through Google (the password path blocks it in LoginService).
        var linked = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (linked is not null && !linked.IsActive)
        {
            return SignInResult.NotAllowed;
        }

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

        return await _userManager.FindByEmailAsync(email.Trim());
    }

    public async Task<SignInResult> LinkAndSignInAsync(User existingUser, ExternalLoginInfo info)
    {
        // Only an active account whose email was confirmed by us may be linked automatically
        // (an unconfirmed local account could have been created by someone else with this email).
        if (!existingUser.IsActive || !existingUser.EmailConfirmed)
        {
            return SignInResult.NotAllowed;
        }

        var add = await _userManager.AddLoginAsync(existingUser, info);
        if (!add.Succeeded)
        {
            _logger.LogWarning("Could not link {Provider} login: {Errors}",
                info.LoginProvider, string.Join("; ", add.Errors.Select(e => e.Code)));
            return SignInResult.Failed;
        }

        // Go through the normal external sign-in so 2FA and lockout are enforced (SignInAsync would skip them).
        return await ExternalLoginSignInAsync(info);
    }

    public async Task<(IdentityResult Result, User? User)> CompletePatientRegistrationAsync(
        ExternalLoginCompleteDto dto,
        ExternalLoginInfo info)
    {
        var email = dto.Email.Trim();

        // Only Patients can be created via Google login.
        var patient = new Patient
        {
            UserName = email,
            Email = email,
            PhoneNumber = dto.PhoneNumber.Trim(),
            FullName = dto.FullName.Trim(),
            Gender = dto.Gender!.Value,
            DateOfBirth = dto.DateOfBirth,
            Role = UserRole.Patient,
            IsActive = true,
            EmailConfirmed = true,   // the email was verified by Google
            CreatedAt = DateTime.UtcNow
        };

        IdentityResult create;
        try
        {
            create = await _userManager.CreateAsync(patient);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "External registration hit a database constraint.");
            return (IdentityResult.Failed(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.DuplicateEmail),
                Description = "An account with this email already exists."
            }), null);
        }

        if (!create.Succeeded)
        {
            return (create, null);
        }

        var role = await _userManager.AddToRoleAsync(patient, nameof(UserRole.Patient));
        if (!role.Succeeded)
        {
            await _userManager.DeleteAsync(patient);   // never leave an account without a role
            return (role, null);
        }

        var login = await _userManager.AddLoginAsync(patient, info);
        if (!login.Succeeded)
        {
            await _userManager.DeleteAsync(patient);   // never leave an account that can't sign in with Google
            return (login, null);
        }

        return (IdentityResult.Success, patient);
    }
}
