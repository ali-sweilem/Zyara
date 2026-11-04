using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zyara.Services.Features.Providers
{
    public interface IProviderSearchService
    {
        Task<PagedResult<ProviderCardDto>> SearchProvidersAsync(ProviderSearchCriteria criteria, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetSpecialtiesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetCitiesAsync(CancellationToken cancellationToken = default);
    }
}

