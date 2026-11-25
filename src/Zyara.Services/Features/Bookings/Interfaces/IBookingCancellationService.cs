using Zyara.Services.Common;
using Zyara.Services.Features.Bookings.Dtos;

namespace Zyara.Services.Features.Bookings.Interfaces;

/// <summary>
/// Cancelling a booking, with the money rules. The patient / provider id ALWAYS comes from ICurrentUser.
///
/// Patient: a request nobody accepted yet is free. A CONFIRMED booking is free until 1 hour before the visit;
///   after that the patient pays the commission (kept from his held card money, or added to his account when
///   nothing was paid) and the same amount is paid to the provider as compensation.
/// Provider: only a CONFIRMED booking. On the day of the visit the commission is deducted from his wallet and the
///   cancellation counts toward a weekly limit; at the limit he receives no new bookings. The patient never pays.
/// </summary>
public interface IBookingCancellationService
{
    Task<ServiceResult<CancellationResultDto>> CancelByPatientAsync(int patientId, int bookingId, CancellationToken cancellationToken = default);

    Task<ServiceResult<CancellationResultDto>> CancelByProviderAsync(int providerId, int bookingId, CancellationToken cancellationToken = default);

    Task<ProviderCancellationStatusDto> GetProviderStatusAsync(int providerId, CancellationToken cancellationToken = default);
}
