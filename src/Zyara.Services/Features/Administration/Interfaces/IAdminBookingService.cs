using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Administration.Dtos;

namespace Zyara.Services.Features.Administration.Interfaces
{
    public interface IAdminBookingService
    {
        /// <summary>All bookings (newest first), optionally filtered by status (null = all).</summary>
        Task<PagedResult<AdminBookingDto>> GetBookingsAsync(BookingStatus? status, int page, int pageSize = 10, CancellationToken cancellationToken = default);
    }
}