using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Administration.Dtos;

namespace Zyara.Services.Features.Administration.Interfaces
{
    public interface IAdminProviderReviewService
    {
        Task<IReadOnlyList<PendingProviderDto>> GetProvidersAsync(VerificationStatus? status = VerificationStatus.Pending, CancellationToken cancellationToken = default);

        /// approve = true: Verified | approve = false: Rejected (reason is required)
        Task<ServiceResult> ReviewProviderAsync(int providerId, bool approve, string? reason, int adminId, CancellationToken cancellationToken = default);
    }
}
