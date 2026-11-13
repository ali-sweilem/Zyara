using Zyara.Services.Common;
using Zyara.Services.Features.Administration.Dtos;

namespace Zyara.Services.Features.Administration.Interfaces
{
    public interface IAdminProviderReviewService
    {
        Task<IReadOnlyList<PendingProviderDto>> GetPendingProvidersAsync(CancellationToken cancellationToken = default);

        /// approve = true: Verified | approve = false: Rejected (reason is required)
        Task<ServiceResult> ReviewProviderAsync(int providerId, bool approve, string? reason, CancellationToken cancellationToken = default);
    }
}