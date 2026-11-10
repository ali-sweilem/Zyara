using System.Collections.Generic;
using Zyara.Services.Features.Providers;

namespace Zyara.Web.Models
{
    public class ProviderDashboardViewModel
    {
        public int ProviderId { get; set; }
        public int TotalBookings { get; set; }
        public int PendingRequests { get; set; }
        public int UpcomingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalEarnings { get; set; }
        public List<BookingRowDto> PendingList { get; set; } = new();
        public List<BookingRowDto> CompletedList { get; set; } = new();
    }
}