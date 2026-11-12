using Zyara.Services.Features.Bookings.Dtos;

namespace Zyara.Services.Features.Bookings.Interfaces;

public interface IBookingService
{
    Task<(bool Succeeded, int? BookingId, string? Error)> CreateBookingAsync(
        int patientId, CreateBookingDto dto, CancellationToken cancellationToken = default);
}

