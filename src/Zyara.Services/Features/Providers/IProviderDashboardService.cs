using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Zyara.Services.Features.Providers
{
    public interface IProviderDashboardService
    {
        Task<ProviderDashboardDto> GetDashboardAsync(int providerId, CancellationToken cancellationToken = default);
    }

    public class ProviderDashboardDto
    {
        public int TotalBookings { get; set; }
        public int PendingRequests { get; set; }
        public int UpcomingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalEarnings { get; set; }
    }
}
