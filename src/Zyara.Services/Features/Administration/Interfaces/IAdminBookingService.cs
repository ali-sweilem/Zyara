using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Administration.Dtos;

namespace Zyara.Services.Features.Administration.Interfaces
{
    public interface IAdminBookingService
    {
        /// <summary>All bookings (newest first), optionally filtered by status and search query.</summary>
        Task<PagedResult<AdminBookingDto>> GetBookingsAsync(BookingStatus? status, string? search = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);

        /// <summary>Cancels an active booking, frees its time slot, and notifies patient &amp; provider.</summary>
        Task<ServiceResult> CancelBookingAsync(int bookingId, CancellationToken cancellationToken = default);
    }
}
