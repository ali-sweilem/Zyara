using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Features.Administration.Dtos;

namespace Zyara.Services.Features.Administration.Interfaces
{
    public interface IAdminServiceCatalogService
    {
        /// <summary>All services (the catalog is small, so no paging).</summary>
        Task<IReadOnlyList<ServiceItemDto>> GetServicesAsync(CancellationToken cancellationToken = default);

        /// <summary>Flips IsActive. Returns false if the service does not exist.</summary>
        Task<bool> ToggleServiceAsync(int id, CancellationToken cancellationToken = default);
    }
}
