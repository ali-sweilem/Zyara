using Zyara.Services.Common;
using Zyara.Services.Features.MedicalRecords.Dtos;

namespace Zyara.Services.Features.MedicalRecords.Interfaces;

/// <summary>
/// Prescription (doctors only) and service note (doctors and nurses) for a booking.
/// Providers can only touch bookings that are theirs and whose visit has started or finished;
/// patients can only read their own bookings.
/// </summary>
public interface IMedicalRecordService
{
    Task<ServiceResult<VisitRecordDto>> GetForProviderAsync(int providerId, int bookingId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SavePrescriptionAsync(int providerId, SavePrescriptionDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> SaveNoteAsync(int providerId, SaveServiceNoteDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult<PatientVisitRecordDto>> GetForPatientAsync(int patientId, int bookingId, CancellationToken cancellationToken = default);
}
