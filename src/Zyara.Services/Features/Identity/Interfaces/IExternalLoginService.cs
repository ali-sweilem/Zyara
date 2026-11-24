using Microsoft.AspNetCore.Identity;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Dtos;

namespace Zyara.Services.Features.Identity.Interfaces;

public interface IExternalLoginService
{
    /// <summary>Sign in with an already-linked external login (blocks deactivated accounts, honours lockout / 2FA / confirmed-email).</summary>
    Task<SignInResult> ExternalLoginSignInAsync(ExternalLoginInfo info);

    Task<User?> FindByEmailAsync(string email);

    /// <summary>Links the external login to an existing, active, email-confirmed account, then signs in through the normal path (so 2FA / lockout still apply).</summary>
    Task<SignInResult> LinkAndSignInAsync(User existingUser, ExternalLoginInfo info);

    /// <summary>Creates a Patient (Google never creates Doctor / Nurse / Admin). Rolls the user back if any later step fails.</summary>
    Task<(IdentityResult Result, User? User)> CompletePatientRegistrationAsync(ExternalLoginCompleteDto dto, ExternalLoginInfo info);
}
