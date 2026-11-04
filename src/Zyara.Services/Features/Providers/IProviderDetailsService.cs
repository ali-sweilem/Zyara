using System.Threading;
using System.Threading.Tasks;

namespace Zyara.Services.Features.Providers;

public interface IProviderDetailsService
{
    Task<ProviderDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<ProviderReviewDto>> GetReviewsAsync(int providerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
