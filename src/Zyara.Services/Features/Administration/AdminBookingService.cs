using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Data;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;

namespace Zyara.Services.Features.Administration
{
    public class AdminBookingService : IAdminBookingService
    {
        private readonly ZyaraDbContext _db;

        public AdminBookingService(ZyaraDbContext db) => _db = db;

        public Task<PagedResult<AdminBookingDto>> GetBookingsAsync(
            BookingStatus? status, int page, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var query = _db.Bookings.AsNoTracking();

            if (status.HasValue)
                query = query.Where(b => b.Status == status.Value);

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
    }
}