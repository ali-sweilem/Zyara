using Microsoft.AspNetCore.Identity;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Dtos;

namespace Zyara.Services.Features.Identity.Interfaces;

public interface IExternalLoginService
{
    Task<SignInResult> ExternalLoginSignInAsync(ExternalLoginInfo info);
    Task<(IdentityResult Result, User? User)> CompletePatientRegistrationAsync(ExternalLoginCompleteDto dto, ExternalLoginInfo info);
    Task<User?> FindByEmailAsync(string email);
}

