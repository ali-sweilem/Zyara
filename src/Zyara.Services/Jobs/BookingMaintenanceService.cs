using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.Notifications.Interfaces;
using Zyara.Services.Features.Shared;

namespace Zyara.Services.Jobs;

public sealed record BookingMaintenanceResult(int Expired, int NoShows);

public interface IBookingMaintenanceService
{
    /// <summary>One pass: expire unanswered requests, mark no-shows. Safe to run repeatedly (idempotent).</summary>
    Task<BookingMaintenanceResult> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The work done by <see cref="BookingMaintenanceWorker"/> every minute.
///  1. PendingProviderApproval + appointment window started          -> Expired (held card money released, patient told)
///  2. Confirmed / ConfirmedCollectionPending + 30 min after window   -> NoShow  (held card money released, both told)
/// </summary>
public sealed class BookingMaintenanceService : IBookingMaintenanceService
{
    private const int BatchSize = 200;

    private readonly IRepository<Booking> _bookings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationService _notifications;

    public BookingMaintenanceService(
        IRepository<Booking> bookings, IUnitOfWork unitOfWork, TimeProvider timeProvider, INotificationService notifications)
    {
        _bookings = bookings;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _notifications = notifications;
    }

    public async Task<BookingMaintenanceResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var expired = await ExpireUnansweredAsync(cancellationToken);
        var noShows = await MarkNoShowsAsync(cancellationToken);
        return new BookingMaintenanceResult(expired, noShows);
    }

    private async Task<int> ExpireUnansweredAsync(CancellationToken ct)
    {
        var now = EgyptTime.Now(_timeProvider);
        var candidates = await LoadCandidatesAsync(
            b => b.Status == BookingStatus.PendingProviderApproval, now, ct);

        var due = candidates.Where(b => BookingTimeRules.IsExpired(SlotStart(b), now)).ToList();
        foreach (var booking in due)
        {
            booking.Status = BookingStatus.Expired;
            ReleaseHeldPayment(booking);
            _notifications.Notify(booking.PatientId, NotificationType.BookingExpired, "Booking expired",
                $"Your booking #{booking.Id} expired because the provider did not respond in time.",
                $"/Booking/Confirmation?id={booking.Id}");
        }

        if (due.Count > 0) await _unitOfWork.SaveChangesAsync(ct);
        return due.Count;
    }

    private async Task<int> MarkNoShowsAsync(CancellationToken ct)
    {
        var now = EgyptTime.Now(_timeProvider);
        var candidates = await LoadCandidatesAsync(
            b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.ConfirmedCollectionPending, now, ct);

        var due = candidates.Where(b => BookingTimeRules.IsNoShow(SlotStart(b), now)).ToList();
        foreach (var booking in due)
        {
            booking.Status = BookingStatus.NoShow;
            ReleaseHeldPayment(booking);   // the service was not delivered: give card money back (cash was never collected)

            _notifications.Notify(booking.PatientId, NotificationType.BookingNoShow, "Visit did not take place",
                $"Your booking #{booking.Id} was marked as a no-show because the visit did not start in time.",
                $"/Booking/Confirmation?id={booking.Id}");
            _notifications.Notify(booking.ProviderId, NotificationType.BookingNoShow, "Booking marked as no-show",
                $"Booking #{booking.Id} was marked as a no-show because it was not started within 30 minutes.",
                "/Provider/Bookings/History");
        }

        if (due.Count > 0) await _unitOfWork.SaveChangesAsync(ct);
        return due.Count;
    }

    /// <summary>Only bookings whose slot day is today or earlier are loaded (the exact minute is checked in memory).</summary>
    private Task<List<Booking>> LoadCandidatesAsync(
        System.Linq.Expressions.Expression<Func<Booking, bool>> statusFilter, DateTime now, CancellationToken ct)
    {
        var today = now.Date;
        return _bookings.Query()
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .Where(statusFilter)
            .Where(b => b.TimeSlot!.Date <= today)
            .OrderBy(b => b.Id)
            .Take(BatchSize)
            .ToListAsync(ct);
    }

    private static DateTime SlotStart(Booking booking) => booking.TimeSlot!.Date.Date + booking.TimeSlot.StartTime;

    private static void ReleaseHeldPayment(Booking booking)
    {
        if (booking.Payment is { Method: PaymentMethod.OnlinePayment, Status: PaymentStatus.Authorized } payment)
            payment.Release();
    }
}
