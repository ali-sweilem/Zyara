
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Domain.Entities;


namespace Zyara.Services.Features.Bookings;

public sealed class BookingService : IBookingService
{
    private readonly ZyaraDbContext _db;

    public BookingService(ZyaraDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PatientBookingDto>> GetMyBookingsAsync(
        int patientId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.PatientId == patientId)
            .OrderByDescending(b => b.TimeSlot!.Date)
            .ThenByDescending(b => b.TimeSlot!.StartTime)
            .Select(b => new PatientBookingDto
            {
                Id = b.Id,
                Status = b.Status,

                ProviderName = b.Provider!.FullName,

                ServiceName = b.Service!.Name,

                BookingDate = b.TimeSlot!.Date,
                StartTime = b.TimeSlot!.StartTime,
                EndTime = b.TimeSlot!.EndTime,

                ServiceFee = b.ServiceFee,
                TotalPrice = b.TotalPrice
            })
            .ToListAsync(cancellationToken);
    }


    public async Task<PatientBookingDto?> GetMyBookingDetailsAsync(
        int bookingId,
        int patientId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.Id == bookingId && b.PatientId == patientId)
            .Select(b => new PatientBookingDto
            {
                Id = b.Id,
                Status = b.Status,

                ProviderName = b.Provider!.FullName,
                ServiceName = b.Service!.Name,

                BookingDate = b.TimeSlot!.Date,
                StartTime = b.TimeSlot!.StartTime,
                EndTime = b.TimeSlot!.EndTime,

                ServiceFee = b.ServiceFee,
                TotalPrice = b.TotalPrice
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BookingCancellationResult> CancelMyBookingAsync(
    int bookingId,
    int patientId,
    CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(
                b => b.Id == bookingId && b.PatientId == patientId,
                cancellationToken);

        if (booking is null)
            return BookingCancellationResult.NotFound;

        if (booking.Status != BookingStatus.PendingProviderApproval &&
            booking.Status != BookingStatus.Confirmed)
        {
            return BookingCancellationResult.NotCancellable;
        }

        booking.Status = BookingStatus.Cancelled;

        if (booking.TimeSlot?.Status == TimeSlotStatus.Booked)
        {
            booking.TimeSlot.Status = TimeSlotStatus.Available;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return BookingCancellationResult.Cancelled;
    }

    public async Task<ReviewSubmissionResult> SubmitRatingAsync(
    int bookingId,
    int patientId,
    int stars,
    string? comment,
    CancellationToken cancellationToken = default)
    {
        if (stars < 1 || stars > 5)
            return ReviewSubmissionResult.InvalidInput;

        comment = string.IsNullOrWhiteSpace(comment)
            ? null
            : comment.Trim();

        if (comment?.Length > 1000)
            return ReviewSubmissionResult.InvalidInput;

        var booking = await _db.Bookings
            .Include(b => b.Provider)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(
                b => b.Id == bookingId && b.PatientId == patientId,
                cancellationToken);

        if (booking is null)
            return ReviewSubmissionResult.NotFound;

        if (booking.Status != BookingStatus.Completed)
            return ReviewSubmissionResult.NotCompleted;

        if (booking.Review is not null)
            return ReviewSubmissionResult.AlreadyReviewed;

        var provider = booking.Provider;

        if (provider is null)
            return ReviewSubmissionResult.NotFound;

        var review = new Review
        {
            BookingId = booking.Id,
            Stars = stars,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };

        _db.Reviews.Add(review);

        provider.AverageRating =
            ((provider.AverageRating * provider.RatingCount) + stars)
            / (provider.RatingCount + 1);

        provider.RatingCount++;

        await _db.SaveChangesAsync(cancellationToken);

        return ReviewSubmissionResult.Submitted;
    }

}
