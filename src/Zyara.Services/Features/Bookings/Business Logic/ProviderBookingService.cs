using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Providers;

namespace Zyara.Services.Features.Bookings;

public sealed class ProviderBookingService : IProviderBookingService
{
    private readonly IRepository<Booking> _bookings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ProviderBookingService(IRepository<Booking> bookings, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _bookings = bookings;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
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
            PaymentStatus = b.Payment != null ? b.Payment.Status : (PaymentStatus?)null
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    // ───────────────────────── helpers ─────────────────────────

    /// <summary>Loads a booking ONLY if it belongs to this provider (otherwise null -> 404, nothing leaked).</summary>
    private Task<Booking?> LoadOwnedAsync(int providerId, int bookingId, CancellationToken ct) =>
        _bookings.Query()
            .Include(b => b.TimeSlot)
            .Include(b => b.Service)
            .Include(b => b.Payment)
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

    private static void Expire(Booking booking)
    {
        booking.Status = BookingStatus.Expired;
        ReleaseHeldPayment(booking);
    }

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
        _ => status.ToString().ToLowerInvariant()
    };

    private static ServiceResult NotFound() => ServiceResult.Fail("Booking not found.");
}
