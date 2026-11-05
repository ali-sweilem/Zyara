namespace Zyara.Web.Models
{
    public class ProviderDashboardViewModel
    {
        public int TotalBookings { get; set; }
        public int PendingRequests { get; set; }
        public int UpcomingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalEarnings { get; set; }
    }
}
