using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Services.Features.Providers.Interfaces
{
    public interface IProviderSearchService
    {
        Task<PagedResult<ProviderCardDto>> SearchProvidersAsync(ProviderSearchCriteria criteria, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetSpecialtiesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetCitiesAsync(CancellationToken cancellationToken = default);

    }
}

