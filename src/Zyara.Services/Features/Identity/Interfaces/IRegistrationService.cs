using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity.Dtos;

namespace Zyara.Services.Features.Identity.Interfaces;

/// <summary>A validation problem; <c>Field</c> is a DTO property name, or empty for a general error.</summary>
public sealed record RegistrationError(string Field, string Message);

public sealed record RegistrationResult(
    bool Succeeded,
    int? UserId,
    UserRole? Role,
    bool SignedIn,
    IReadOnlyList<RegistrationError> Errors)
{
    public static RegistrationResult Failed(params RegistrationError[] errors) =>
        new(false, null, null, false, errors);
}

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterPatientAsync(RegisterPatientDto dto);

    /// <summary>Creates a Doctor or Nurse (Pending verification). Never creates an Admin.</summary>
    Task<RegistrationResult> RegisterProviderAsync(RegisterProviderDto dto);
}
