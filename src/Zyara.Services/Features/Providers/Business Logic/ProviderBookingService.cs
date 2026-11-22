using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Repositories.Data;
using Zyara.Services.Common;
using Zyara.Services.Features.Bookings;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services.Features.Providers;

public class ProviderBookingService : IProviderBookingService
{
    private readonly ZyaraDbContext _db;
    private readonly IBookingTransitions _transitions;
    private readonly TimeProvider _timeProvider;

    public ProviderBookingService(
        ZyaraDbContext db,
        IBookingTransitions transitions,
        TimeProvider timeProvider)
    {
        _db = db;
        _transitions = transitions;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<ProviderRequestCardDto>> GetRequestsAsync(
        int providerId,
        CancellationToken cancellationToken = default)
    {
        var egyptNow = EgyptTime.Now(_timeProvider);

        var bookings = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.Service)
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .Where(b => b.ProviderId == providerId && b.Status == BookingStatus.PendingProviderApproval)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        return bookings.Select(b =>
        {
            var windowStart = b.TimeSlot != null ? b.TimeSlot.Date.Date + b.TimeSlot.StartTime : DateTime.MaxValue;
            var approvalDeadlineUtc = b.CreatedAt.AddHours(BookingRules.DefaultPendingApprovalExpiryHours);
            var approvalDeadlineEgypt = EgyptTime.ConvertUtcToEgypt(approvalDeadlineUtc);
            var effectiveDeadline = approvalDeadlineEgypt < windowStart ? approvalDeadlineEgypt : windowStart;

            var diff = effectiveDeadline - egyptNow;
            var minutesLeft = (int)Math.Max(0, diff.TotalMinutes);
            var hoursLeft = minutesLeft / 60;
            var display = minutesLeft <= 0 
                ? "Expiring now" 
                : hoursLeft > 0 ? $"{hoursLeft}h {minutesLeft % 60}m left" : $"{minutesLeft}m left";

            return new ProviderRequestCardDto
            {
                BookingId = b.Id,
                PatientName = b.Patient?.FullName ?? "Patient",
                PatientNotes = b.Notes,
                ServiceName = b.Service?.Name ?? "Service",
                ServiceType = b.Service?.Type ?? ServiceType.DoctorHomeVisit,
                Date = b.TimeSlot?.Date ?? egyptNow.Date,
                StartTime = b.TimeSlot?.StartTime ?? TimeSpan.Zero,
                EndTime = b.TimeSlot?.EndTime ?? TimeSpan.FromHours(2),
                FormattedWindow = b.TimeSlot != null ? SlotFormatting.FormatWindow(b.TimeSlot.StartTime, b.TimeSlot.EndTime) : "",
                DayLabel = b.TimeSlot != null ? SlotFormatting.FormatDayLabel(b.TimeSlot.Date, egyptNow) : "",
                PaymentMethod = b.Payment?.Method ?? PaymentMethod.CashOnArrival,
                PaymentStatus = b.Payment?.Status ?? PaymentStatus.Pending,
                ServiceFee = b.ServiceFee,
                Commission = b.Commission,
                TotalPrice = b.TotalPrice,
                TimeLeftMinutes = minutesLeft,
                TimeLeftDisplay = display
            };
        }).ToList();
    }

    public async Task<PagedResult<ProviderVisitItemDto>> GetVisitsAsync(
        int providerId,
        string group,
        string? statusFilter,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var egyptNow = EgyptTime.Now(_timeProvider);
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.Service)
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .Where(b => b.ProviderId == providerId);

        var activeGroup = (group ?? "upcoming").ToLowerInvariant();

        switch (activeGroup)
        {
            case "inprogress":
                query = query.Where(b => b.Status == BookingStatus.Started);
                break;
            case "history":
                query = query.Where(b => b.Status == BookingStatus.Completed ||
                                         b.Status == BookingStatus.Cancelled ||
                                         b.Status == BookingStatus.Rejected ||
                                         b.Status == BookingStatus.Expired);
                if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<BookingStatus>(statusFilter, ignoreCase: true, out var parsedStatus))
                {
                    query = query.Where(b => b.Status == parsedStatus);
                }
                if (fromDate.HasValue)
                {
                    var f = fromDate.Value.Date;
                    query = query.Where(b => b.TimeSlot != null && b.TimeSlot.Date >= f);
                }
                if (toDate.HasValue)
                {
                    var t = toDate.Value.Date;
                    query = query.Where(b => b.TimeSlot != null && b.TimeSlot.Date <= t);
                }
                break;
            case "upcoming":
            default:
                activeGroup = "upcoming";
                query = query.Where(b => b.Status == BookingStatus.Confirmed);
                break;
        }

        query = activeGroup == "history"
            ? query.OrderByDescending(b => b.TimeSlot!.Date).ThenByDescending(b => b.TimeSlot!.StartTime)
            : query.OrderBy(b => b.TimeSlot!.Date).ThenBy(b => b.TimeSlot!.StartTime);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(b =>
        {
            var isPrivacyCleared = b.Status == BookingStatus.Confirmed ||
                                   b.Status == BookingStatus.Started ||
                                   b.Status == BookingStatus.Completed;

            return new ProviderVisitItemDto
            {
                BookingId = b.Id,
                PatientName = b.Patient?.FullName ?? "Patient",
                PatientPhone = isPrivacyCleared ? b.Patient?.PhoneNumber : null,
                PatientAddress = isPrivacyCleared ? b.VisitAddress : null,
                PatientLatitude = isPrivacyCleared ? b.VisitLatitude : null,
                PatientLongitude = isPrivacyCleared ? b.VisitLongitude : null,
                ServiceName = b.Service?.Name ?? "Service",
                ServiceType = b.Service?.Type ?? ServiceType.DoctorHomeVisit,
                Date = b.TimeSlot?.Date ?? egyptNow.Date,
                StartTime = b.TimeSlot?.StartTime ?? TimeSpan.Zero,
                EndTime = b.TimeSlot?.EndTime ?? TimeSpan.FromHours(2),
                FormattedWindow = b.TimeSlot != null ? SlotFormatting.FormatWindow(b.TimeSlot.StartTime, b.TimeSlot.EndTime) : "",
                DayLabel = b.TimeSlot != null ? SlotFormatting.FormatDayLabel(b.TimeSlot.Date, egyptNow) : "",
                Status = b.Status,
                StatusReason = b.StatusReason,
                PaymentMethod = b.Payment?.Method ?? PaymentMethod.CashOnArrival,
                PaymentStatus = b.Payment?.Status ?? PaymentStatus.Pending,
                TotalPrice = b.TotalPrice,
                NetEarnings = b.TotalPrice - b.ServiceFee - b.Commission,
                IsHomeVisit = b.Service != null && b.Service.Type != ServiceType.RemoteConsultation,
                OtpCode = b.Status == BookingStatus.Started ? b.OtpCode : null,
                OtpFailedAttempts = b.OtpFailedAttempts
            };
        }).ToList();

        return new PagedResult<ProviderVisitItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<ProviderVisitDetailsDto?> GetVisitDetailsAsync(
        int providerId,
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        var b = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.Service)
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId, cancellationToken);

        if (b is null) return null;

        var egyptNow = EgyptTime.Now(_timeProvider);
        var isPrivacyCleared = b.Status == BookingStatus.Confirmed ||
                               b.Status == BookingStatus.Started ||
                               b.Status == BookingStatus.Completed;

        var isHomeVisit = b.Service != null && b.Service.Type != ServiceType.RemoteConsultation;

        return new ProviderVisitDetailsDto
        {
            BookingId = b.Id,
            Status = b.Status,
            StatusReason = b.StatusReason,
            IsActive = b.IsActive,
            PatientId = b.PatientId,
            PatientName = b.Patient?.FullName ?? "Patient",
            PatientPhone = isPrivacyCleared ? b.Patient?.PhoneNumber : null,
            PatientAddress = isPrivacyCleared ? b.VisitAddress : null,
            PatientLatitude = isPrivacyCleared ? b.VisitLatitude : null,
            PatientLongitude = isPrivacyCleared ? b.VisitLongitude : null,
            CanSeePatientContact = isPrivacyCleared,
            ServiceId = b.ServiceId,
            ServiceName = b.Service?.Name ?? "Service",
            ServiceType = b.Service?.Type ?? ServiceType.DoctorHomeVisit,
            ServiceDescription = b.Service?.Description,
            Date = b.TimeSlot?.Date ?? egyptNow.Date,
            StartTime = b.TimeSlot?.StartTime ?? TimeSpan.Zero,
            EndTime = b.TimeSlot?.EndTime ?? TimeSpan.FromHours(2),
            FormattedWindow = b.TimeSlot != null ? SlotFormatting.FormatWindow(b.TimeSlot.StartTime, b.TimeSlot.EndTime) : "",
            DayLabel = b.TimeSlot != null ? SlotFormatting.FormatDayLabel(b.TimeSlot.Date, egyptNow) : "",
            PatientNotes = b.Notes,
            PaymentMethod = b.Payment?.Method ?? PaymentMethod.CashOnArrival,
            PaymentStatus = b.Payment?.Status ?? PaymentStatus.Pending,
            TotalPrice = b.TotalPrice,
            ServiceFee = b.ServiceFee,
            Commission = b.Commission,
            CreatedAtEgypt = EgyptTime.ConvertUtcToEgypt(b.CreatedAt),
            RespondedAtEgypt = b.RespondedAt.HasValue ? EgyptTime.ConvertUtcToEgypt(b.RespondedAt.Value) : null,
            StartedAtEgypt = b.StartedAt.HasValue ? EgyptTime.ConvertUtcToEgypt(b.StartedAt.Value) : null,
            CompletedAtEgypt = b.CompletedAt.HasValue ? EgyptTime.ConvertUtcToEgypt(b.CompletedAt.Value) : null,
            IsHomeVisit = isHomeVisit,
            OtpCode = isHomeVisit ? b.OtpCode : null,
            OtpFailedAttempts = b.OtpFailedAttempts
        };
    }

    public async Task<ServiceResult<bool>> AcceptAsync(int providerId, int bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId, cancellationToken);

        if (booking == null) return ServiceResult<bool>.Fail("Booking not found or access denied.");
        if (booking.Status != BookingStatus.PendingProviderApproval)
            return ServiceResult<bool>.Fail($"Cannot accept booking from status {booking.Status}.");

        return await _transitions.AcceptAsync(booking, DateTime.UtcNow, cancellationToken);
    }

    public async Task<ServiceResult<bool>> RejectAsync(int providerId, int bookingId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10 || reason.Trim().Length > 300)
        {
            return ServiceResult<bool>.Fail("A rejection reason must be between 10 and 300 characters.");
        }

        var booking = await _db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId, cancellationToken);

        if (booking == null) return ServiceResult<bool>.Fail("Booking not found or access denied.");
        if (booking.Status != BookingStatus.PendingProviderApproval)
            return ServiceResult<bool>.Fail($"Cannot reject booking from status {booking.Status}.");

        return await _transitions.RejectAsync(booking, reason.Trim(), DateTime.UtcNow, cancellationToken);
    }

    public async Task<ServiceResult<bool>> StartAsync(int providerId, int bookingId, string? otp, CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Service)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId, cancellationToken);

        if (booking == null) return ServiceResult<bool>.Fail("Booking not found or access denied.");
        if (booking.Status != BookingStatus.Confirmed)
            return ServiceResult<bool>.Fail($"Visit can only be started from Confirmed status (current status: {booking.Status}).");

        if (booking.TimeSlot == null)
            return ServiceResult<bool>.Fail("Booking has no linked arrival window.");

        // Check time tolerance relative to the arrival window (Egypt time)
        var egyptNow = EgyptTime.Now(_timeProvider);
        var windowStart = booking.TimeSlot.Date.Date + booking.TimeSlot.StartTime;
        var windowEnd = booking.TimeSlot.Date.Date + booking.TimeSlot.EndTime;
        var earliest = windowStart.AddMinutes(-BookingRules.StartVisitEarlyMinutes);
        var latest = windowEnd.AddMinutes(BookingRules.StartVisitLateMinutes);

        if (egyptNow < earliest || egyptNow > latest)
        {
            return ServiceResult<bool>.Fail(
                $"Visit can only be started between 60 minutes before the window start and 120 minutes after the window end (allowed: {earliest:hh:mm tt} to {latest:hh:mm tt}).");
        }

        // OTP verification for home visits
        var isHomeVisit = booking.Service != null && booking.Service.Type != ServiceType.RemoteConsultation;
        if (isHomeVisit)
        {
            if (booking.OtpFailedAttempts >= BookingRules.OtpMaxFailedAttempts)
            {
                return ServiceResult<bool>.Fail("Maximum OTP verification attempts exceeded. Starting this visit is blocked. Please contact support.");
            }

            var cleanOtp = (otp ?? "").Trim();
            if (string.IsNullOrWhiteSpace(cleanOtp) || cleanOtp != booking.OtpCode)
            {
                booking.OtpFailedAttempts++;
                await _db.SaveChangesAsync(cancellationToken);
                var remaining = BookingRules.OtpMaxFailedAttempts - booking.OtpFailedAttempts;
                return ServiceResult<bool>.Fail(remaining > 0
                    ? $"Invalid verification OTP. {remaining} attempt(s) remaining."
                    : "Maximum OTP verification attempts exceeded. Starting this visit is blocked. Please contact support.");
            }
        }

        return await _transitions.StartAsync(booking, DateTime.UtcNow, cancellationToken);
    }

    public async Task<ServiceResult<bool>> CompleteAsync(int providerId, int bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Service)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId, cancellationToken);

        if (booking == null) return ServiceResult<bool>.Fail("Booking not found or access denied.");
        if (booking.Status != BookingStatus.Started)
            return ServiceResult<bool>.Fail($"Visit can only be completed from Started status (current status: {booking.Status}).");

        return await _transitions.CompleteAsync(booking, DateTime.UtcNow, cancellationToken);
    }

    public async Task<ServiceResult<bool>> CancelAsync(int providerId, int bookingId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10 || reason.Trim().Length > 300)
        {
            return ServiceResult<bool>.Fail("A cancellation reason must be between 10 and 300 characters.");
        }

        var booking = await _db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId, cancellationToken);

        if (booking == null) return ServiceResult<bool>.Fail("Booking not found or access denied.");
        if (booking.Status != BookingStatus.Confirmed)
            return ServiceResult<bool>.Fail($"Only Confirmed bookings can be cancelled by the provider (current status: {booking.Status}).");

        return await _transitions.CancelByProviderAsync(booking, reason.Trim(), DateTime.UtcNow, cancellationToken);
    }
}
