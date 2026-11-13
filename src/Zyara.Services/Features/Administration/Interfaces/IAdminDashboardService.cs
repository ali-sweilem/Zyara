using Zyara.Services.Features.AdminDashboard.Dtos;

namespace Zyara.Services.Features.AdminDashboard.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetSummaryAsync();
}