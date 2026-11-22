using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Services.Features.Providers.Interfaces;

public interface IProviderDashboardService
{
    Task<ProviderDashboardDto?> GetDashboardAsync(int providerId, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(int providerId, CancellationToken cancellationToken = default);
}

