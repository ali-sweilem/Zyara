using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Common;
using Zyara.Services.Features.Patients.Dtos;

namespace Zyara.Services.Features.Patients;

public interface IPatientPortalService
{
    Task<PatientDashboardDto?> GetDashboardAsync(int patientId, CancellationToken cancellationToken = default);
}
