using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

public sealed class RegistrationService : IRegistrationService
{
    private const string DuplicateEmailMessage = "An account with this email already exists.";

    private readonly UserManager<User> _users;
    private readonly SignInManager<User> _signIn;
    private readonly IOptions<IdentityOptions> _identityOptions;
    private readonly IEmailConfirmationService _emailConfirmation;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        UserManager<User> users,
        SignInManager<User> signIn,
        IOptions<IdentityOptions> identityOptions,
        IEmailConfirmationService emailConfirmation,
        ILogger<RegistrationService> logger)
    {
        _users = users;
        _signIn = signIn;
        _identityOptions = identityOptions;
        _emailConfirmation = emailConfirmation;
        _logger = logger;
    }

    public Task<RegistrationResult> RegisterPatientAsync(RegisterPatientDto dto)
    {
        var patient = new Patient { DateOfBirth = dto.DateOfBirth };
        ApplyCommon(patient, dto, UserRole.Patient);
        return CreateAsync(patient, dto.Password, UserRole.Patient);
    }

    public Task<RegistrationResult> RegisterProviderAsync(RegisterProviderDto dto)
    {
        // The role is NEVER taken from the form as-is: only Doctor / Nurse are allowed (no Admin, no Patient).
        if (dto.ProviderType is not (UserRole.Doctor or UserRole.Nurse))
        {
            return Task.FromResult(RegistrationResult.Failed(
                new RegistrationError(nameof(RegisterProviderDto.ProviderType), "Choose Doctor or Nurse.")));
        }

        var role = dto.ProviderType.Value;
        Provider provider;

        if (role == UserRole.Doctor)
        {
            var license = dto.LicenseNumber?.Trim();
            if (string.IsNullOrEmpty(license))
            {
                return Task.FromResult(RegistrationResult.Failed(
                    new RegistrationError(nameof(RegisterProviderDto.LicenseNumber), "License number is required for doctors.")));
            }

            provider = new Doctor { licenser = license };   // field name kept as-is (renaming needs a migration)
        }
        else
        {
            provider = new Nurse();
        }

        ApplyCommon(provider, dto, role);
        provider.Specialty = dto.Specialty.Trim();
        provider.ExperienceYears = dto.ExperienceYears;
        provider.BaseCity = dto.BaseCity.Trim();
        provider.VerificationStatus = VerificationStatus.Pending;

        return CreateAsync(provider, dto.Password, role);
    }

    private static void ApplyCommon(User user, RegisterBaseDto dto, UserRole role)
    {
        var email = dto.Email.Trim();
        user.UserName = email;
        user.Email = email;
        user.PhoneNumber = dto.Phone.Trim();
        user.FullName = dto.FullName.Trim();
        user.Gender = dto.Gender!.Value;
        user.Role = role;
        user.IsActive = true;
        user.EmailConfirmed = false;
    }

    private async Task<RegistrationResult> CreateAsync(User user, string password, UserRole role)
    {
        IdentityResult create;
        try
        {
            create = await _users.CreateAsync(user, password);   // hashes the password
        }
        catch (DbUpdateException ex)
        {
            // Two requests registering the same email at the same moment (unique index on Email).
            _logger.LogWarning(ex, "Registration hit a database constraint.");
            return RegistrationResult.Failed(new RegistrationError(nameof(RegisterBaseDto.Email), DuplicateEmailMessage));
        }

        if (!create.Succeeded)
        {
            return RegistrationResult.Failed(MapErrors(create.Errors).Distinct().ToArray());
        }

        var addRole = await _users.AddToRoleAsync(user, role.ToString());
        if (!addRole.Succeeded)
        {
            // Don't leave an account without a role behind.
            await _users.DeleteAsync(user);
            _logger.LogError("Could not assign role {Role} to a new user: {Errors}",
                role, string.Join("; ", addRole.Errors.Select(e => e.Code)));
            return RegistrationResult.Failed(new RegistrationError("", "Registration failed. Please try again."));
        }

        // T7: email the confirmation link. A mail failure must not undo the registration
        // (the user can use "Resend" later).
        try
        {
            await _emailConfirmation.SendAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not send the confirmation email after registration.");
        }

        // Once RequireConfirmedEmail = true (T7) the user must confirm first, so we don't sign them in.
        var signedIn = false;
        if (!_identityOptions.Value.SignIn.RequireConfirmedEmail)
        {
            await _signIn.SignInAsync(user, isPersistent: false);
            signedIn = true;
        }

        return new RegistrationResult(true, user.Id, role, signedIn, Array.Empty<RegistrationError>());
    }

    private static IEnumerable<RegistrationError> MapErrors(IEnumerable<IdentityError> errors)
    {
        foreach (var e in errors)
        {
            if (e.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName))
            {
                yield return new RegistrationError(nameof(RegisterBaseDto.Email), DuplicateEmailMessage);
            }
            else if (e.Code == nameof(IdentityErrorDescriber.InvalidEmail) || e.Code == nameof(IdentityErrorDescriber.InvalidUserName))
            {
                yield return new RegistrationError(nameof(RegisterBaseDto.Email), "Enter a valid email address.");
            }
            else if (e.Code.StartsWith("Password", StringComparison.Ordinal))
            {
                yield return new RegistrationError(nameof(RegisterBaseDto.Password), e.Description);
            }
            else
            {
                yield return new RegistrationError("", e.Description);
            }
        }
    }
}
