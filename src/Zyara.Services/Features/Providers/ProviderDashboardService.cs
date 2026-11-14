using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Data;

namespace Zyara.Services.Features.Providers
{
    public sealed class ProviderDashboardService : IProviderDashboardService
    {
        private readonly ZyaraDbContext _db;

        public ProviderDashboardService(ZyaraDbContext db)
        {
            _db = db;
        }

        public async Task<ProviderDashboardDto> GetDashboardAsync(int providerId, CancellationToken cancellationToken = default)
        {
            var bookings = _db.Bookings.AsNoTracking().Where(b => b.ProviderId == providerId);

            var pendingList = await bookings
                .Where(b => b.Status == BookingStatus.PendingProviderApproval)
                .OrderBy(b => b.TimeSlot!.Date).ThenBy(b => b.TimeSlot!.StartTime)
                .Select(b => new BookingRowDto
                {
                    BookingId = b.Id,
                    PatientName = b.Patient!.FullName,
                    ServiceName = b.Service!.Name,
                    Date = b.TimeSlot!.Date,
                    StartTime = b.TimeSlot!.StartTime,
                    TotalPrice = b.TotalPrice
                })
                .ToListAsync(cancellationToken);

            var completedList = await bookings
                .Where(b => b.Status == BookingStatus.Completed)
                .OrderByDescending(b => b.TimeSlot!.Date).ThenByDescending(b => b.TimeSlot!.StartTime)
                .Take(20)
                .Select(b => new BookingRowDto
                {
                    BookingId = b.Id,
                    PatientName = b.Patient!.FullName,
                    ServiceName = b.Service!.Name,
                    Date = b.TimeSlot!.Date,
                    StartTime = b.TimeSlot!.StartTime,
                    TotalPrice = b.TotalPrice
                })
                .ToListAsync(cancellationToken);

            return new ProviderDashboardDto
            {
                TotalBookings = await bookings.CountAsync(cancellationToken),
                PendingRequests = await bookings.CountAsync(b => b.Status == BookingStatus.PendingProviderApproval, cancellationToken),
                UpcomingBookings = await bookings.CountAsync(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.ConfirmedCollectionPending, cancellationToken),
                CompletedBookings = await bookings.CountAsync(b => b.Status == BookingStatus.Completed, cancellationToken),
                TotalEarnings = await bookings.Where(b => b.Status == BookingStatus.Completed).SumAsync(b => b.TotalPrice - b.Commission, cancellationToken),
                PendingList = pendingList,
                CompletedList = completedList
            };
        }

        public async Task<ServiceResult> AcceptBookingAsync(int providerId, int bookingId, CancellationToken cancellationToken = default)
        {
            var booking = await _db.Bookings
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId, cancellationToken);

            if (booking is null)
                return ServiceResult.Fail("Booking not found.");
            if (booking.Status != BookingStatus.PendingProviderApproval)
                return ServiceResult.Fail("This request is no longer pending.");

            booking.Status = booking.Payment?.Method == PaymentMethod.CashOnArrival
                ? BookingStatus.ConfirmedCollectionPending
                : BookingStatus.Confirmed;

            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> RejectBookingAsync(int providerId, int bookingId, CancellationToken cancellationToken = default)
        {
            var booking = await _db.Bookings
                .Include(b => b.Payment)
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId, cancellationToken);

            if (booking is null)
                return ServiceResult.Fail("Booking not found.");
            if (booking.Status != BookingStatus.PendingProviderApproval)
                return ServiceResult.Fail("This request is no longer pending.");

            booking.Status = BookingStatus.Rejected;

            if (booking.TimeSlot is not null)
                booking.TimeSlot.Status = TimeSlotStatus.Available;

            if (booking.Payment is { Method: PaymentMethod.OnlinePayment, Status: PaymentStatus.Authorized })
                booking.Payment.Release();

            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok();
        }
    }
}