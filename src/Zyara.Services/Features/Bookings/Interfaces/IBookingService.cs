using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Common;
using Zyara.Services.Features.Bookings.Dtos;

namespace Zyara.Services.Features.Bookings.Interfaces;

public interface IBookingService
{
    /// <summary>
    /// Returns available (unbooked) time slots for the given provider.
    /// Excludes slots that are already Booked or Locked, and excludes past slots for today.
    /// </summary>
    Task<IReadOnlyList<BookingSlotDto>> GetAvailableSlotsAsync(
        int providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a booking for the given patient against the chosen provider service and time slot.
    /// Validates: provider exists, slot is free, service belongs to provider, date is not in the past.
    /// Returns the new Booking ID on success, or an error message on failure.
    /// </summary>
    Task<ServiceResult<int>> CreateBookingAsync(
        BookAppointmentDto dto,
        CancellationToken cancellationToken = default);
}


