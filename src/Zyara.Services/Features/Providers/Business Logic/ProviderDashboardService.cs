using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Repositories.Data;
using Zyara.Services.Common;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services.Features.Providers;

public class ProviderDashboardService : IProviderDashboardService
{
    private readonly ZyaraDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ProviderDashboardService(ZyaraDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<int> GetPendingCountAsync(int providerId, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .CountAsync(b => b.ProviderId == providerId && b.Status == BookingStatus.PendingProviderApproval, cancellationToken);
    }

    public async Task<ProviderDashboardDto?> GetDashboardAsync(int providerId, CancellationToken cancellationToken = default)
    {
        var provider = await _db.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);

        if (provider is null) return null;

        var egyptNow = EgyptTime.Now(_timeProvider);
        var today = egyptNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var nextMonth = startOfMonth.AddMonths(1);

        // 1. KPIs
        var pendingRequestsCount = await _db.Bookings
            .AsNoTracking()
            .CountAsync(b => b.ProviderId == providerId && b.Status == BookingStatus.PendingProviderApproval, cancellationToken);

        var todayVisitsCount = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.TimeSlot)
            .CountAsync(b => b.ProviderId == providerId &&
                             (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Started) &&
                             b.TimeSlot != null && b.TimeSlot.Date == today, cancellationToken);

        var completedThisMonthBookings = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.TimeSlot)
            .Where(b => b.ProviderId == providerId &&
                        b.Status == BookingStatus.Completed &&
                        b.TimeSlot != null &&
                        b.TimeSlot.Date >= startOfMonth &&
                        b.TimeSlot.Date < nextMonth)
            .ToListAsync(cancellationToken);

        var completedThisMonthCount = completedThisMonthBookings.Count;
        var netEarningsThisMonth = completedThisMonthBookings.Sum(b => b.TotalPrice - b.ServiceFee - b.Commission);

        // 2. Requests waiting for you (latest 5)
        var pendingRequests = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.Service)
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .Where(b => b.ProviderId == providerId && b.Status == BookingStatus.PendingProviderApproval)
            .OrderBy(b => b.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var requestCards = pendingRequests.Select(b =>
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
                Date = b.TimeSlot?.Date ?? today,
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

        // 3. Today & upcoming visits timeline (next 7 days)
        var endOfUpcoming = today.AddDays(7);
        var upcomingVisits = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.Service)
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .Where(b => b.ProviderId == providerId &&
                        (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Started) &&
                        b.TimeSlot != null &&
                        b.TimeSlot.Date >= today &&
                        b.TimeSlot.Date <= endOfUpcoming)
            .OrderBy(b => b.TimeSlot!.Date)
            .ThenBy(b => b.TimeSlot!.StartTime)
            .ToListAsync(cancellationToken);

        var visitItems = upcomingVisits.Select(b => new ProviderVisitItemDto
        {
            BookingId = b.Id,
            PatientName = b.Patient?.FullName ?? "Patient",
            PatientPhone = b.Patient?.PhoneNumber,
            PatientAddress = b.VisitAddress,
            PatientLatitude = b.VisitLatitude,
            PatientLongitude = b.VisitLongitude,
            ServiceName = b.Service?.Name ?? "Service",
            ServiceType = b.Service?.Type ?? ServiceType.DoctorHomeVisit,
            Date = b.TimeSlot?.Date ?? today,
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
            OtpCode = b.OtpCode,
            OtpFailedAttempts = b.OtpFailedAttempts
        }).ToList();

        // 4. Schedule health: count of available windows in next 7 days
        var availableWindowsNext7Days = await _db.TimeSlots
            .AsNoTracking()
            .Where(ts => ts.ProviderId == providerId &&
                         ts.Status == TimeSlotStatus.Available &&
                         ts.Date >= today &&
                         ts.Date <= endOfUpcoming)
            .CountAsync(cancellationToken);

        // 5. Latest 3 reviews
        var latestReviews = await _db.Reviews
            .AsNoTracking()
            .Include(r => r.Booking)
                .ThenInclude(b => b.Patient)
            .Where(r => r.Booking.ProviderId == providerId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(3)
            .Select(r => new ProviderReviewDto
            {
                ReviewerName = r.Booking.Patient.FullName,
                Rating = r.Stars,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ProviderDashboardDto
        {
            ProviderId = provider.Id,
            FullName = provider.FullName,
            Role = provider.Role,
            Specialty = provider.Specialty,
            PhotoUrl = provider.PhotoUrl,
            VerificationStatus = provider.VerificationStatus,
            RejectionReason = provider.RejectionReason,
            TodayDateEgyptFormatted = egyptNow.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture),
            PendingRequestsCount = pendingRequestsCount,
            TodayVisitsCount = todayVisitsCount,
            CompletedThisMonthCount = completedThisMonthCount,
            NetEarningsThisMonth = netEarningsThisMonth,
            AverageRating = provider.AverageRating,
            RatingCount = provider.RatingCount,
            WalletBalance = provider.WalletBalance,
            RequestsWaiting = requestCards,
            TodayAndUpcomingVisits = visitItems,
            AvailableWindowsNext7Days = availableWindowsNext7Days,
            LatestReviews = latestReviews
        };
    }
}
