using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Notifications.Interfaces;
using Zyara.Services.Features.Reviews.Dtos;
using Zyara.Services.Features.Reviews.Interfaces;

namespace Zyara.Services.Features.Reviews;

public sealed class ReviewService : IReviewService
{
    private const string NotFound = "Visit not found.";
    private const string NotCompleted = "Only completed visits can be reviewed.";
    private const string AlreadyReviewed = "You already reviewed this visit.";

    private readonly IRepository<Booking> _bookings;
    private readonly IRepository<Review> _reviews;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notifications;

    public ReviewService(
        IRepository<Booking> bookings,
        IRepository<Review> reviews,
        IUnitOfWork unitOfWork,
        INotificationService notifications)
    {
        _bookings = bookings;
        _reviews = reviews;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
    }

    public async Task<PatientReviewsPageDto> GetPageAsync(int patientId, CancellationToken cancellationToken = default)
    {
        var rows = await _bookings.Query()
            .AsNoTracking()
            .Where(b => b.PatientId == patientId && b.Status == BookingStatus.Completed)
            .OrderByDescending(b => b.TimeSlot!.Date)
            .ThenByDescending(b => b.TimeSlot!.StartTime)
            .Select(b => new
            {
                BookingId = b.Id,
                ProviderName = b.Provider!.FullName,
                ProviderPhotoUrl = b.Provider.PhotoUrl,
                ProviderSpecialty = b.Provider.Specialty,
                ServiceName = b.Service!.Name,
                b.TimeSlot!.Date,
                b.TimeSlot.StartTime,
                b.TimeSlot.EndTime,
                Stars = b.Review != null ? b.Review.Stars : 0,
                Comment = b.Review != null ? b.Review.Comment : null,
                ReviewedAt = b.Review != null ? b.Review.CreatedAt : (DateTime?)null
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        var page = new PatientReviewsPageDto();

        foreach (var r in rows)
        {
            if (r.ReviewedAt is null)
            {
                page.Pending.Add(new ReviewableVisitDto
                {
                    BookingId = r.BookingId,
                    ProviderName = r.ProviderName,
                    ProviderPhotoUrl = r.ProviderPhotoUrl,
                    ProviderSpecialty = r.ProviderSpecialty,
                    ServiceName = r.ServiceName,
                    Date = r.Date,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime
                });
            }
            else
            {
                page.Reviewed.Add(new MyReviewDto
                {
                    BookingId = r.BookingId,
                    ProviderName = r.ProviderName,
                    ProviderPhotoUrl = r.ProviderPhotoUrl,
                    ProviderSpecialty = r.ProviderSpecialty,
                    ServiceName = r.ServiceName,
                    Date = r.Date,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    Stars = r.Stars,
                    Comment = r.Comment,
                    ReviewedAt = r.ReviewedAt.Value
                });
            }
        }

        return page;
    }

    public async Task<ServiceResult<ReviewFormDto>> GetFormAsync(
        int patientId, int bookingId, CancellationToken cancellationToken = default)
    {
        // PatientId in the filter: somebody else's booking id is simply "not found" (nothing leaked).
        var row = await _bookings.Query()
            .AsNoTracking()
            .Where(b => b.Id == bookingId && b.PatientId == patientId)
            .Select(b => new
            {
                b.Status,
                HasReview = b.Review != null,
                ProviderName = b.Provider!.FullName,
                ProviderPhotoUrl = b.Provider.PhotoUrl,
                ProviderSpecialty = b.Provider.Specialty,
                ServiceName = b.Service!.Name,
                b.TimeSlot!.Date,
                b.TimeSlot.StartTime,
                b.TimeSlot.EndTime
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null) return ServiceResult<ReviewFormDto>.Fail(NotFound);
        if (row.Status != BookingStatus.Completed) return ServiceResult<ReviewFormDto>.Fail(NotCompleted);
        if (row.HasReview) return ServiceResult<ReviewFormDto>.Fail(AlreadyReviewed);

        return ServiceResult<ReviewFormDto>.Ok(new ReviewFormDto
        {
            BookingId = bookingId,
            ProviderName = row.ProviderName,
            ProviderPhotoUrl = row.ProviderPhotoUrl,
            ProviderSpecialty = row.ProviderSpecialty,
            ServiceName = row.ServiceName,
            Date = row.Date,
            StartTime = row.StartTime,
            EndTime = row.EndTime
        });
    }

    public async Task<ServiceResult> SubmitAsync(int patientId, CreateReviewDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Stars is < 1 or > 5)
        {
            return ServiceResult.Fail("Please choose a rating from 1 to 5 stars.");
        }

        var booking = await _bookings.Query()
            .Include(b => b.Provider)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId && b.PatientId == patientId, cancellationToken);

        if (booking?.Provider is null) return ServiceResult.Fail(NotFound);
        if (booking.Status != BookingStatus.Completed) return ServiceResult.Fail(NotCompleted);
        if (booking.Review is not null) return ServiceResult.Fail(AlreadyReviewed);

        var provider = booking.Provider;
        var comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim();

        try
        {
            // The review and the provider's new rating are saved together or not at all.
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                _reviews.Add(new Review { BookingId = booking.Id, Stars = dto.Stars, Comment = comment });

                _notifications.Notify(provider.Id, NotificationType.General, "New review",
                    $"A patient rated booking #{booking.Id} with {dto.Stars} star(s).", $"/Providers/Details/{provider.Id}#reviews");

                await _unitOfWork.SaveChangesAsync(ct);

                // Recalculate from ALL the provider's reviews (never "old average + 1"), so it can't drift.
                var all = _reviews.Query().Where(r => r.Booking.ProviderId == provider.Id);
                var count = await all.CountAsync(ct);
                var average = await all.AverageAsync(r => (double)r.Stars, ct);

                provider.RatingCount = count;
                provider.AverageRating = Math.Round(average, 2);
                await _unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // unique index on Reviews.BookingId: the review was submitted twice (double click / two tabs)
            return ServiceResult.Fail(AlreadyReviewed);
        }

        return ServiceResult.Ok();
    }
}
