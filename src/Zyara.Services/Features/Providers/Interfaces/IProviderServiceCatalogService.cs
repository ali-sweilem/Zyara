using Zyara.Services.Common;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Services.Features.Providers.Interfaces;

/// <summary>
/// The services a provider offers to patients (and his price for each).
/// Everything is scoped to <c>providerId</c> (from ICurrentUser): a provider can never touch someone else's offers.
/// </summary>
public interface IProviderServiceCatalogService
{
    Task<ProviderServicesPageDto> GetPageAsync(int providerId, CancellationToken cancellationToken = default);

    Task<ServiceResult> AddAsync(int providerId, AddOfferedServiceDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateFeeAsync(int providerId, UpdateOfferedServiceDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> RemoveAsync(int providerId, int providerServiceId, CancellationToken cancellationToken = default);
}
