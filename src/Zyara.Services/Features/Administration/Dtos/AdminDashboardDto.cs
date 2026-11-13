namespace Zyara.Services.Features.AdminDashboard.Dtos;

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int PendingProviders { get; set; }
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CompletedBookings { get; set; }
}