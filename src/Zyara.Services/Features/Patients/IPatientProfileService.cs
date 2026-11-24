using Zyara.Services.Common;
using Zyara.Services.Features.Patients.Dtos;

namespace Zyara.Services.Features.Patients;

/// <summary>The patient's own profile. patientId always comes from ICurrentUser, never from a form or URL.</summary>
public interface IPatientProfileService
{
    Task<PatientProfileDto?> GetAsync(int patientId, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(int patientId, EditPatientProfileDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> AddAddressAsync(int patientId, AddAddressDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAddressAsync(int patientId, int addressId, CancellationToken cancellationToken = default);

    /// <summary>Changes the password. This device stays signed in, every other device is signed out.</summary>
    Task<ServiceResult> ChangePasswordAsync(int patientId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
}
