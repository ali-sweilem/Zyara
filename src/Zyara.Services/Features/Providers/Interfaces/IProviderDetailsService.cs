using Zyara.Services.Common;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Services.Features.Providers.Interfaces;

public interface IProviderDetailsService
{
    Task<ProviderDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<ProviderReviewDto>> GetReviewsAsync(int providerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    //=================================================profile==========================================================================
    Task<ProviderProfileDto?> showprofile(int providerId);
    Task<EditProfileProviderDto?> GetForEditAsync(int id);
    Task<bool> UpdateProfileAsync(EditProfileProviderDto model);
}
