using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Common;


namespace Zyara.Services.Features.Providers
{
    public interface IProviderDashboardService
    {
        Task<ProviderDashboardDto> GetDashboardAsync(int providerId, CancellationToken cancellationToken = default);
        Task<ServiceResult> AcceptBookingAsync(int providerId, int bookingId, CancellationToken cancellationToken = default);
        Task<ServiceResult> RejectBookingAsync(int providerId, int bookingId, CancellationToken cancellationToken = default);
    }

    public class ProviderDashboardDto
    {
        public int TotalBookings { get; set; }
        public int PendingRequests { get; set; }
        public int UpcomingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalEarnings { get; set; }
        public List<BookingRowDto> PendingList { get; set; } = new();
        public List<BookingRowDto> CompletedList { get; set; } = new();
    }
    public class BookingRowDto
    {
        public int BookingId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
