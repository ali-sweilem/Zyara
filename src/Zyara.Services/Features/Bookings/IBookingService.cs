using Zyara.Domain.Entities;
using Zyara.Services.Common;

namespace Zyara.Services.Features.Bookings;

public interface IBookingService
{
    Task<PagedResult<Booking>> GetProviderBookingsAsync(int providerId, BookingFilter filter);
    Task<ServiceResult<BookingDetailsResult>> GetBookingDetailsAsync(int providerId, int bookingId);
    Task<ServiceResult> AcceptAsync(int providerId, int bookingId);
    Task<ServiceResult> RejectAsync(int providerId, int bookingId);
    Task<ServiceResult> StartAsync(int providerId, int bookingId, string otp);
    Task<ServiceResult> CompleteAsync(int providerId, int bookingId);
    Task<ServiceResult> CancelAsync(int providerId, int bookingId);
    Task<ServiceResult> AddServiceNoteAsync(int providerId, int bookingId, string content);
}
