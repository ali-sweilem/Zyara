using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
using Zyara.Services.Features.AdminDashboard.Dtos;
using Zyara.Services.Features.AdminDashboard.Interfaces;

namespace Zyara.Services.Features.AdminDashboard;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly ZyaraDbContext _db;
    public AdminDashboardService(ZyaraDbContext db) => _db = db;

    public async Task<AdminDashboardDto> GetSummaryAsync()
    {
        return new AdminDashboardDto
        {
            TotalUsers = await _db.Users.CountAsync(),
            ActiveUsers = await _db.Users.CountAsync(u => u.IsActive),
            PendingProviders = await _db.Providers.CountAsync(p => p.VerificationStatus == VerificationStatus.Pending),
            TotalBookings = await _db.Bookings.CountAsync(),
            PendingBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.PendingProviderApproval),
            CompletedBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Completed)
        };
    }
}