using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Administration.Dtos;

namespace Zyara.Services.Features.Administration.Interfaces
{
    public interface IAdminServiceCatalogService
    {
        /// <summary>All services (the catalog is small, so no paging).</summary>
        Task<IReadOnlyList<ServiceItemDto>> GetServicesAsync(CancellationToken cancellationToken = default);

        /// <summary>Flips IsActive. Returns false if the service does not exist.</summary>
        Task<bool> ToggleServiceAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Adds a service to the catalog (providers then pick it and set their own price).</summary>
        Task<ServiceResult> CreateServiceAsync(string? name, string? description, ServiceType type, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing service's name, description, and type.</summary>
        Task<ServiceResult> UpdateServiceAsync(int id, string? name, string? description, ServiceType type, CancellationToken cancellationToken = default);
    }
}

