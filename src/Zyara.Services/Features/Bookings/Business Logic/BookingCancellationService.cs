using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Notifications.Interfaces;
using Zyara.Services.Features.Wallet.Interfaces;
using SharedTime = Zyara.Services.Features.Shared.EgyptTime;

namespace Zyara.Services.Features.Bookings;

public sealed class BookingCancellationService : IBookingCancellationService
{
    private const string NotFound = "Booking not found.";

    private readonly IRepository<Booking> _bookings;
    private readonly IRepository<Provider> _providers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationService _notifications;
    private readonly IWalletService _wallet;

    public BookingCancellationService(
        IRepository<Booking> bookings,
        IRepository<Provider> providers,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        INotificationService notifications,
        IWalletService wallet)
    {
        _bookings = bookings;
        _providers = providers;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _notifications = notifications;
        _wallet = wallet;
    }

    // ───────────────────────── patient ─────────────────────────

    public async Task<ServiceResult<CancellationResultDto>> CancelByPatientAsync(
        int patientId, int bookingId, CancellationToken cancellationToken = default)
    {
        // PatientId in the filter: somebody else's booking is simply "not found".
        var booking = await _bookings.Query()
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .Include(b => b.Patient)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.PatientId == patientId, cancellationToken);

        if (booking?.TimeSlot is null || booking.Patient is null)
            return ServiceResult<CancellationResultDto>.Fail(NotFound);

        var pending = booking.Status == BookingStatus.PendingProviderApproval;
        var confirmed = booking.Status is BookingStatus.Confirmed or BookingStatus.ConfirmedCollectionPending;
        if (!pending && !confirmed)
            return ServiceResult<CancellationResultDto>.Fail("This booking cannot be cancelled at its current status.");

        // Only a booking the provider already accepted can cost money, and only in the last hour before the visit.
        var now = SharedTime.Now(_timeProvider);
        var fee = confirmed && CancellationRules.IsLatePatientCancel(SlotStart(booking), now) ? booking.Commission : 0m;

        var payment = booking.Payment;
        var heldOnline = payment is { Method: PaymentMethod.OnlinePayment, Status: PaymentStatus.Authorized };
        string message;

        if (fee <= 0m)
        {
            fee = 0m;
            if (heldOnline) payment!.Release();   // the held card money goes back in full
            message = "Booking cancelled. No penalty was charged.";
        }
        else if (heldOnline)
        {
            // keep the penalty from the held card money and give the rest back
            fee = Math.Min(fee, payment!.Amount);
            payment.Capture();
            if (fee < payment.Amount) payment.Refund(payment.Amount - fee);
            message = $"Booking cancelled. A penalty of {fee:0.##} EGP was kept from your card payment and the rest was returned to you.";
        }
        else
        {
            // cash (or a card payment that was never completed): nothing to take now, so it becomes a debt on his account
            booking.Patient.OutstandingPenalty += fee;
            message = $"Booking cancelled. A penalty of {fee:0.##} EGP was added to your account and will be collected with your next completed visit.";
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledBy = CancelledBy.Patient;
        booking.CancelledAt = _timeProvider.GetUtcNow().UtcDateTime;
        booking.CancellationFee = fee;

        // The provider's slot can be booked again (needs the filtered unique index on Bookings.TimeSlotId).
        booking.TimeSlot.Status = TimeSlotStatus.Available;

        // The penalty is paid to the provider as compensation (the platform keeps nothing of it).
        var delta = fee > 0m
            ? _wallet.RecordEntry(booking.ProviderId, booking.Id, WalletTransactionType.CancellationCompensation, fee,
                $"Booking #{booking.Id}: the patient cancelled less than 1 hour before the visit")
            : 0m;

        _notifications.Notify(booking.ProviderId, NotificationType.BookingCancelled, "Booking cancelled by the patient",
            fee > 0m
                ? $"Booking #{booking.Id} was cancelled less than 1 hour before the visit. {fee:0.##} EGP was added to your wallet as compensation."
                : $"Booking #{booking.Id} was cancelled by the patient.",
            "/Provider/Bookings/History");

        await SaveAsync(booking.ProviderId, delta, cancellationToken);
        return ServiceResult<CancellationResultDto>.Ok(new CancellationResultDto { Fee = fee, Message = message });
    }

    // ───────────────────────── provider ─────────────────────────

    public async Task<ServiceResult<CancellationResultDto>> CancelByProviderAsync(
        int providerId, int bookingId, CancellationToken cancellationToken = default)
    {
        // ProviderId in the filter: a provider can only cancel his own bookings.
        var booking = await _bookings.Query()
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .Include(b => b.Provider)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId, cancellationToken);

        if (booking?.TimeSlot is null || booking.Provider is null)
            return ServiceResult<CancellationResultDto>.Fail(NotFound);

        if (booking.Status is not (BookingStatus.Confirmed or BookingStatus.ConfirmedCollectionPending))
            return ServiceResult<CancellationResultDto>.Fail(
                "Only a confirmed booking can be cancelled. A new request can be rejected instead.");

        var now = SharedTime.Now(_timeProvider);
        var sameDay = CancellationRules.IsSameDay(SlotStart(booking), now);
        var fee = sameDay ? booking.Commission : 0m;

        // The patient never pays for the provider's cancellation: held card money goes back in full.
        var releasedOnline = booking.Payment is { Method: PaymentMethod.OnlinePayment, Status: PaymentStatus.Authorized };
        if (releasedOnline) booking.Payment!.Release();

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledBy = CancelledBy.Provider;
        booking.CancelledAt = _timeProvider.GetUtcNow().UtcDateTime;
        booking.CancellationFee = fee;

        // The slot is NOT freed on purpose: the provider cancelled that time, so patients must not book it again.

        var provider = booking.Provider;
        var delta = 0m;
        var suspended = false;
        var countThisWeek = 0;

        if (sameDay)
        {
            var weekStart = CancellationRules.WeekStart(now);
            countThisWeek = CancellationRules.EffectiveWeeklyCount(provider.SameDayCancellationCount, provider.CancellationWeekStart, now) + 1;
            provider.SameDayCancellationCount = countThisWeek;
            provider.CancellationWeekStart = weekStart;

            if (fee > 0m)
            {
                delta = _wallet.RecordEntry(providerId, booking.Id, WalletTransactionType.CancellationPenalty, -fee,
                    $"Booking #{booking.Id}: you cancelled on the day of the visit");
            }

            if (countThisWeek >= CancellationRules.ProviderSuspendAfter)
            {
                // paused until the week ends (the count restarts then); an admin can lift it earlier
                provider.BookingsSuspendedUntilUtc = SharedTime.ConvertEgyptToUtc(weekStart.AddDays(7));
                suspended = true;
            }
        }

        _notifications.Notify(booking.PatientId, NotificationType.BookingCancelled, "Booking cancelled by the provider",
            releasedOnline
                ? $"Sorry, your booking #{booking.Id} was cancelled by the provider. Your payment was released."
                : $"Sorry, your booking #{booking.Id} was cancelled by the provider.",
            $"/Booking/Confirmation?id={booking.Id}");

        if (suspended)
        {
            _notifications.Notify(providerId, NotificationType.General, "New bookings are paused",
                $"You cancelled {countThisWeek} bookings on the day of the visit this week, so patients cannot book you until the new week starts.",
                "/Provider/Bookings");
        }

        await SaveAsync(providerId, delta, cancellationToken);

        var text = sameDay
            ? (fee > 0m ? $"Booking cancelled. A penalty of {fee:0.##} EGP was deducted from your wallet." : "Booking cancelled.")
              + $" This is your same-day cancellation {countThisWeek} of {CancellationRules.ProviderSuspendAfter} this week."
              + (suspended ? " New bookings are now paused until the new week starts, or until an admin resets it." : string.Empty)
            : "Booking cancelled. No penalty, because it was not the day of the visit. The patient was notified.";

        return ServiceResult<CancellationResultDto>.Ok(new CancellationResultDto { Fee = fee, Message = text });
    }

    public async Task<ProviderCancellationStatusDto> GetProviderStatusAsync(
        int providerId, CancellationToken cancellationToken = default)
    {
        var p = await _providers.Query()
            .AsNoTracking()
            .Where(x => x.Id == providerId)
            .Select(x => new { x.SameDayCancellationCount, x.CancellationWeekStart, x.BookingsSuspendedUntilUtc })
            .FirstOrDefaultAsync(cancellationToken);

        if (p is null) return new ProviderCancellationStatusDto();

        var now = SharedTime.Now(_timeProvider);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var suspended = p.BookingsSuspendedUntilUtc is { } until && until > utcNow;

        return new ProviderCancellationStatusDto
        {
            CountThisWeek = CancellationRules.EffectiveWeeklyCount(p.SameDayCancellationCount, p.CancellationWeekStart, now),
            IsSuspended = suspended,
            SuspendedUntil = suspended ? SharedTime.ConvertUtcToEgypt(p.BookingsSuspendedUntilUtc!.Value) : null
        };
    }

    // ───────────────────────── helpers ─────────────────────────

    /// <summary>Saves everything and moves the wallet balance in the same transaction.</summary>
    private Task SaveAsync(int providerId, decimal delta, CancellationToken cancellationToken) =>
        _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _unitOfWork.SaveChangesAsync(ct);
            if (delta != 0m) await _wallet.ApplyBalanceDeltaAsync(providerId, delta, ct);
        }, cancellationToken);

    private static DateTime SlotStart(Booking booking) => booking.TimeSlot!.Date.Date + booking.TimeSlot.StartTime;
}
