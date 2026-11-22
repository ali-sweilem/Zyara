using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Domain.Entities;
using Zyara.Services.Common;
using Zyara.Services.Features.Bookings.Dtos;

namespace Zyara.Services.Features.Bookings.Interfaces;

public interface IBookingService
{
    Task<Booking?> GetBookingAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns available (unbooked) time slots for the given provider adhering to BookingRules.
    /// </summary>
    Task<IReadOnlyList<BookingSlotDto>> GetAvailableSlotsAsync(
        int providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a booking for the given patient against the chosen provider service and time slot.
    /// In one transaction: validates provider, slot, service, address, locks slot, creates payment, generates OTP.
    /// </summary>
    Task<ServiceResult<CreateBookingResult>> CreateBookingAsync(
        CreateBookingCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns saved addresses for the patient.
    /// </summary>
    Task<IReadOnlyList<AddressDto>> GetPatientAddressesAsync(
        int patientId,
        CancellationToken cancellationToken = default);
}
