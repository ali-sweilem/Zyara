using Zyara.Services.Features.Identity.Dtos;

namespace Zyara.Services.Features.Identity.Interfaces;

public interface ILoginService
{
    /// <summary>
    /// Checks the password (with lockout), email confirmation and IsActive, then signs the user in.
    /// Unknown email and wrong password return the same status (no user enumeration).
    /// </summary>
    Task<LoginResultDto> LoginAsync(LoginDto dto);

    Task LogoutAsync();
}
