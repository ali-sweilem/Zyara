using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Common;

namespace Zyara.Services.Features.Services;

public interface IServiceManagementService
{
    Task<IReadOnlyList<ServiceDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ServiceDto>> CreateAsync(
        CreateServiceDto dto,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ServiceDto>> UpdateAsync(
        int id,
        UpdateServiceDto dto,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> SetActiveStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}