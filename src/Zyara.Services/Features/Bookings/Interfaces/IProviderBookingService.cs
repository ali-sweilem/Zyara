using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Bookings.Dtos;

namespace Zyara.Services.Features.Bookings.Interfaces;

/// <summary>
/// The provider side of the booking lifecycle:
/// PendingProviderApproval -> Confirmed -> Started -> Completed  (or Rejected / Expired).
/// Every method is scoped to <c>providerId</c> (taken from ICurrentUser, never from a form/URL),
/// so a provider can never touch another provider's booking.
/// </summary>
public interface IProviderBookingService
{
    /// <summary>Booking requests page: pending requests + accepted bookings still to be done.</summary>
    Task<ProviderBookingsPageDto> GetRequestsAsync(int providerId, CancellationToken cancellationToken = default);

    /// <summary>History page: ALL of the provider's bookings (any status), newest first, optional status filter.</summary>
    Task<PagedResult<ProviderBookingItemDto>> GetHistoryAsync(
        int providerId, BookingStatus? status, int page, int pageSize = 10, CancellationToken cancellationToken = default);

    Task<ServiceResult> AcceptAsync(int providerId, int bookingId, CancellationToken cancellationToken = default);

    Task<ServiceResult> RejectAsync(int providerId, int bookingId, CancellationToken cancellationToken = default);

    /// <summary>Home visits need the OTP the patient received; remote consultations start without one.</summary>
    Task<ServiceResult> StartAsync(int providerId, int bookingId, string? otp, CancellationToken cancellationToken = default);

    Task<ServiceResult> CompleteAsync(int providerId, int bookingId, CancellationToken cancellationToken = default);
}
