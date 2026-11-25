using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Notifications.Interfaces;
using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Wallet.Interfaces;

namespace Zyara.Services.Features.Bookings;

public sealed class ProviderBookingService : IProviderBookingService
{
    private readonly IRepository<Booking> _bookings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationService _notifications;
    private readonly IWalletService _wallet;

    public ProviderBookingService(
        IRepository<Booking> bookings, IUnitOfWork unitOfWork, TimeProvider timeProvider,
        INotificationService notifications, IWalletService wallet)
    {
        _wallet = wallet;
        _bookings = bookings;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _notifications = notifications;
    }

    // ───────────────────────── lists ─────────────────────────

    private static readonly System.Linq.Expressions.Expression<Func<Booking, ProviderBookingItemDto>> ToItem = b =>
        new ProviderBookingItemDto
        {
            Id = b.Id,
            PatientName = b.Patient!.FullName,
            PatientPhone = b.Patient.PhoneNumber,
            ServiceName = b.Service!.Name,
            IsHomeVisit = b.Service.Type != ServiceType.RemoteConsultation,
            Date = b.TimeSlot!.Date,
            StartTime = b.TimeSlot.StartTime,
            EndTime = b.TimeSlot.EndTime,
            Status = b.Status,
            TotalPrice = b.TotalPrice,
            PaymentMethod = b.Payment != null ? b.Payment.Method : (PaymentMethod?)null,
            PaymentStatus = b.Payment != null ? b.Payment.Status : (PaymentStatus?)null,
            Commission = b.Commission,
            PatientOutstandingPenalty = b.Patient.OutstandingPenalty
        };

    public async Task<ProviderBookingsPageDto> GetRequestsAsync(int providerId, CancellationToken cancellationToken = default)
    {
        await ExpireStaleAsync(providerId, cancellationToken);

        var rows = await _bookings.Query()
            .AsNoTracking()
            .Where(b => b.ProviderId == providerId &&
                        (b.Status == BookingStatus.PendingProviderApproval
                         || b.Status == BookingStatus.Confirmed
                         || b.Status == BookingStatus.ConfirmedCollectionPending
                         || b.Status == BookingStatus.Started))
            .Select(ToItem)
            .ToListAsync(cancellationToken);

        // what cancelling would cost right now (only on the day of the visit)
        var now = EgyptTime.Now(_timeProvider);
        foreach (var row in rows)
        {
            row.CancelPenaltyNow = CancellationRules.IsSameDay(row.Date.Date + row.StartTime, now) ? row.Commission : 0m;
        }

        return new ProviderBookingsPageDto
        {
            Pending = rows.Where(r => r.Status == BookingStatus.PendingProviderApproval)
                          .OrderBy(r => r.Date).ThenBy(r => r.StartTime).ToList(),
            Active = rows.Where(r => r.Status != BookingStatus.PendingProviderApproval)
                         .OrderBy(r => r.Date).ThenBy(r => r.StartTime).ToList()
        };
    }

    public async Task<PagedResult<ProviderBookingItemDto>> GetHistoryAsync(
        int providerId, BookingStatus? status, int page, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        await ExpireStaleAsync(providerId, cancellationToken);

        var query = _bookings.Query()
            .AsNoTracking()
            .Where(b => b.ProviderId == providerId);

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        return await query
            .OrderByDescending(b => b.TimeSlot!.Date)
            .ThenByDescending(b => b.TimeSlot!.StartTime)
            .ThenByDescending(b => b.Id)
            .ToPagedAsync(ToItem, page, pageSize, maxPageSize: 50, cancellationToken: cancellationToken);
    }

    // ───────────────────────── transitions ─────────────────────────

    public async Task<ServiceResult> AcceptAsync(int providerId, int bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await LoadOwnedAsync(providerId, bookingId, cancellationToken);
        if (booking is null) return NotFound();

        if (booking.Status != BookingStatus.PendingProviderApproval)
            return ServiceResult.Fail($"This booking is already {Describe(booking.Status)}.");

        if (SlotStart(booking) <= EgyptTime.Now(_timeProvider))
        {
            Expire(booking);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Fail("The appointment time has already passed, so the booking expired.");
        }

        booking.Status = BookingStatus.Confirmed;
        NotifyPatient(booking, NotificationType.BookingAccepted, "Booking accepted",
            $"Your booking #{booking.Id} was accepted by the provider.");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RejectAsync(int providerId, int bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await LoadOwnedAsync(providerId, bookingId, cancellationToken);
        if (booking is null) return NotFound();

        if (booking.Status != BookingStatus.PendingProviderApproval)
            return ServiceResult.Fail($"This booking is already {Describe(booking.Status)}.");

        booking.Status = BookingStatus.Rejected;
        ReleaseHeldPayment(booking);
        NotifyPatient(booking, NotificationType.BookingRejected, "Booking rejected",
            $"Sorry, your booking #{booking.Id} was rejected by the provider.");

        // NOTE: the slot stays "Booked": Bookings.TimeSlotId has an unfiltered UNIQUE index, so a new
        // booking on the same slot would be rejected by the database (needs a filtered index + migration).
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> StartAsync(int providerId, int bookingId, string? otp, CancellationToken cancellationToken = default)
    {
        var booking = await LoadOwnedAsync(providerId, bookingId, cancellationToken);
        if (booking is null) return NotFound();

        if (booking.Status is not (BookingStatus.Confirmed or BookingStatus.ConfirmedCollectionPending))
            return ServiceResult.Fail($"A {Describe(booking.Status)} booking cannot be started.");

        if (booking.TimeSlot!.Date.Date > EgyptTime.Now(_timeProvider).Date)
            return ServiceResult.Fail("You can only start a booking on the day of the appointment.");

        var isHomeVisit = booking.Service!.Type != ServiceType.RemoteConsultation;
        if (isHomeVisit)
        {
            if (string.IsNullOrEmpty(booking.OtpCode))
                return ServiceResult.Fail("The patient has not received the verification code yet.");

            if (!OtpMatches(booking.OtpCode, otp))
                return ServiceResult.Fail("The verification code is incorrect.");
        }

        booking.Status = BookingStatus.Started;
        NotifyPatient(booking, NotificationType.VisitStarted, "Visit started",
            $"Your visit for booking #{booking.Id} has started.");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CompleteAsync(int providerId, int bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await LoadOwnedAsync(providerId, bookingId, cancellationToken);
        if (booking is null) return NotFound();

        if (booking.Status != BookingStatus.Started)
            return ServiceResult.Fail("Only a started booking can be completed.");

        booking.Status = BookingStatus.Completed;
        SettlePayment(booking);
        _notifications.Notify(booking.PatientId, NotificationType.VisitCompleted, "Visit completed",
            $"Your booking #{booking.Id} is completed. How was your visit? Tap to rate it.",
            $"/Reviews/Create?bookingId={booking.Id}");

        // Wallet: ledger line (tracked) + relative balance update, all in ONE transaction with the status change.
        var delta = _wallet.RecordBookingSettlement(booking);

        // An earlier cancellation penalty of this patient (cash / unpaid bookings) is collected by the provider with this
        // visit. It belongs to the platform, so it is debited from the provider's wallet (BookingId stays null: the
        // ledger allows only one line per booking, and this booking already has its settlement line).
        var penalty = booking.Patient?.OutstandingPenalty ?? 0m;
        if (penalty > 0m)
        {
            booking.Patient!.OutstandingPenalty = 0m;
            delta += _wallet.RecordEntry(booking.ProviderId, null, WalletTransactionType.PenaltyCollection, -penalty,
                $"Booking #{booking.Id}: earlier cancellation penalty collected from the patient (belongs to the platform)");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _unitOfWork.SaveChangesAsync(ct);
            if (delta != 0m) await _wallet.ApplyBalanceDeltaAsync(booking.ProviderId, delta, ct);
        }, cancellationToken);

        return ServiceResult.Ok();
    }

    // ───────────────────────── helpers ─────────────────────────

    /// <summary>Loads a booking ONLY if it belongs to this provider (otherwise null -> 404, nothing leaked).</summary>
    private Task<Booking?> LoadOwnedAsync(int providerId, int bookingId, CancellationToken ct) =>
        _bookings.Query()
            .Include(b => b.TimeSlot)
            .Include(b => b.Service)
            .Include(b => b.Payment)
            .Include(b => b.Patient)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId, ct);

    /// <summary>Pending bookings whose appointment time passed without an answer become Expired.</summary>
    private async Task ExpireStaleAsync(int providerId, CancellationToken ct)
    {
        var pending = await _bookings.Query()
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .Where(b => b.ProviderId == providerId && b.Status == BookingStatus.PendingProviderApproval)
            .ToListAsync(ct);

        var now = EgyptTime.Now(_timeProvider);
        var stale = pending.Where(b => SlotStart(b) <= now).ToList();
        if (stale.Count == 0) return;

        foreach (var booking in stale) Expire(booking);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private void Expire(Booking booking)
    {
        booking.Status = BookingStatus.Expired;
        ReleaseHeldPayment(booking);
        NotifyPatient(booking, NotificationType.BookingExpired, "Booking expired",
            $"Your booking #{booking.Id} expired because the provider did not respond in time.");
    }

    private void NotifyPatient(Booking booking, NotificationType type, string title, string message) =>
        _notifications.Notify(booking.PatientId, type, title, message, $"/Booking/Confirmation?id={booking.Id}");

    /// <summary>Online payments are only authorized (held) until the visit is done: give the money back on reject/expire.</summary>
    private static void ReleaseHeldPayment(Booking booking)
    {
        if (booking.Payment is { Method: PaymentMethod.OnlinePayment, Status: PaymentStatus.Authorized } payment)
            payment.Release();
    }

    /// <summary>Cash is collected, online authorized money is captured. Guarded so it never throws on odd states.</summary>
    private static void SettlePayment(Booking booking)
    {
        var payment = booking.Payment;
        if (payment is null) return;

        if (payment is { Method: PaymentMethod.CashOnArrival, Status: PaymentStatus.Pending })
            payment.Collect();
        else if (payment is { Method: PaymentMethod.OnlinePayment, Status: PaymentStatus.Authorized })
            payment.Capture();
    }

    private static DateTime SlotStart(Booking booking) => booking.TimeSlot!.Date.Date + booking.TimeSlot.StartTime;

    private static bool OtpMatches(string expected, string? provided)
    {
        if (string.IsNullOrWhiteSpace(provided)) return false;

        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(provided.Trim());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static string Describe(BookingStatus status) => status switch
    {
        BookingStatus.PendingProviderApproval => "waiting for approval",
        BookingStatus.ConfirmedCollectionPending => "confirmed",
        BookingStatus.NoShow => "marked as a no-show",
        _ => status.ToString().ToLowerInvariant()
    };

    private static ServiceResult NotFound() => ServiceResult.Fail("Booking not found.");
}
