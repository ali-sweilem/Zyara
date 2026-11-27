using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Common;
using Zyara.Services.Features.Patients.Dtos;

namespace Zyara.Services.Features.Patients;

public interface ICareRecipientService
{
    Task<IReadOnlyList<CareRecipientDto>> ListAsync(int patientId, CancellationToken cancellationToken = default);

    Task<CareRecipientDto?> GetAsync(int patientId, int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> AddAsync(int patientId, AddCareRecipientDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(int patientId, int id, EditCareRecipientDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(int patientId, int id, CancellationToken cancellationToken = default);
}

