using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
using Microsoft.EntityFrameworkCore;

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

            return new ProviderDashboardDto
            {
                TotalBookings = await bookings.CountAsync(cancellationToken),
                PendingRequests = await bookings.CountAsync(b => b.Status == BookingStatus.PendingProviderApproval, cancellationToken),
                UpcomingBookings = await bookings.CountAsync(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.ConfirmedCollectionPending, cancellationToken),
                CompletedBookings = await bookings.CountAsync(b => b.Status == BookingStatus.Completed, cancellationToken),
                TotalEarnings = await bookings.Where(b => b.Status == BookingStatus.Completed).SumAsync(b => b.TotalPrice - b.Commission, cancellationToken)
            };
        }
    }
}
