using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Identity.Dtos;

public enum LoginStatus
{
    Succeeded,
    InvalidCredentials,   // also used for unknown email and for deactivated accounts (one generic message)
    EmailNotConfirmed,
    LockedOut,
    RequiresTwoFactor
}

public sealed record LoginResultDto(LoginStatus Status, UserRole? Role = null)
{
    public bool Succeeded => Status == LoginStatus.Succeeded;

    public static LoginResultDto Failed(LoginStatus status) => new(status);
}
