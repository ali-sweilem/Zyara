using Microsoft.AspNetCore.Identity;
using Zyara.Services.Features.Identity.Dtos;

namespace Zyara.Services.Features.Identity.Interfaces;

public interface IPasswordRecoveryService
{
    Task ForgotPasswordAsync(string email, string originSchemeAndHost);
    Task<IdentityResult> ResetPasswordAsync(ResetPasswordDto dto);
}

