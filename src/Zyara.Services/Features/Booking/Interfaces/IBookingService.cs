
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings;

namespace Zyara.Services.Features.Bookings.Interfaces;

public interface IBookingService
{
    Task<IReadOnlyList<PatientBookingDto>> GetMyBookingsAsync(
        int patientId,
        CancellationToken cancellationToken = default);

    Task<PatientBookingDto?> GetMyBookingDetailsAsync(
        int bookingId,
        int patientId,
        CancellationToken cancellationToken = default);

    Task<BookingCancellationResult> CancelMyBookingAsync(
    int bookingId,
    int patientId,
    CancellationToken cancellationToken = default);

    Task<ReviewSubmissionResult> SubmitRatingAsync(
    int bookingId,
    int patientId,
    int stars,
    string? comment,
    CancellationToken cancellationToken = default);

}
