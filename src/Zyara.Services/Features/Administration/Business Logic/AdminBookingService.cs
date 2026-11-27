using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Repositories.Data;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;
using Zyara.Services.Features.Notifications.Interfaces;

namespace Zyara.Services.Features.Administration
{
    public class AdminBookingService : IAdminBookingService
    {
        private readonly ZyaraDbContext _db;
        private readonly INotificationService _notifications;
        private readonly IUnitOfWork _unitOfWork;

        public AdminBookingService(
            ZyaraDbContext db,
            INotificationService notifications,
            IUnitOfWork unitOfWork)
        {
            _db = db;
            _notifications = notifications;
            _unitOfWork = unitOfWork;
        }

        public Task<PagedResult<AdminBookingDto>> GetBookingsAsync(
            BookingStatus? status,
            string? search = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Bookings.AsNoTracking();

            if (status.HasValue)
                query = query.Where(b => b.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                if (int.TryParse(term.TrimStart('#'), out var bookingId))
                {
                    query = query.Where(b =>
                        b.Id == bookingId ||
                        b.Patient!.FullName.Contains(term) ||
                        b.Provider!.FullName.Contains(term));
                }
                else
                {
                    query = query.Where(b =>
                        b.Patient!.FullName.Contains(term) ||
                        b.Provider!.FullName.Contains(term) ||
                        b.Service!.Name.Contains(term));
                }
            }

            return query
                .OrderByDescending(b => b.TimeSlot!.Date)
                .ThenByDescending(b => b.Id)
                .ToPagedAsync(
                    b => new AdminBookingDto
                    {
                        Id = b.Id,
                        PatientName = b.Patient!.FullName,
                        ProviderName = b.Provider!.FullName,
                        ServiceName = b.Service!.Name,
                        Date = b.TimeSlot!.Date,
                        StartTime = b.TimeSlot.StartTime,
                        TotalPrice = b.TotalPrice,
                        Status = b.Status,
                        PaymentStatus = b.Payment != null ? b.Payment.Status : (PaymentStatus?)null
                    },
                    page,
                    pageSize,
                    maxPageSize: 50,
                    cancellationToken: cancellationToken);
        }

        public async Task<ServiceResult> CancelBookingAsync(int bookingId, CancellationToken cancellationToken = default)
        {
            var booking = await _db.Bookings
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking is null)
                return ServiceResult.Fail("Booking not found.");

            if (booking.Status is not (BookingStatus.PendingProviderApproval or BookingStatus.Confirmed or BookingStatus.ConfirmedCollectionPending))
                return ServiceResult.Fail("Only pending or confirmed bookings can be cancelled.");

            booking.Status = BookingStatus.Cancelled;
            if (booking.TimeSlot is not null)
                booking.TimeSlot.Status = TimeSlotStatus.Available;

            _notifications.Notify(
                booking.PatientId,
                NotificationType.BookingCancelled,
                "Booking Cancelled by Admin",
                $"Your booking #{booking.Id} was cancelled by platform administration.",
                "/Patient");

            _notifications.Notify(
                booking.ProviderId,
                NotificationType.BookingCancelled,
                "Booking Cancelled by Admin",
                $"Booking #{booking.Id} was cancelled by platform administration and the time slot is now available.",
                "/Provider/Bookings/History");

            await _unitOfWork.ExecuteInTransactionAsync(
                ct => _unitOfWork.SaveChangesAsync(ct), cancellationToken);

            return ServiceResult.Ok();
        }
    }
}
