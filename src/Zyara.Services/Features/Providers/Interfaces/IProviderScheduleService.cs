using System;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Common;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Services.Features.Providers.Interfaces;

public interface IProviderScheduleService
{
    Task<ProviderScheduleViewDto> GetScheduleAsync(int providerId, bool includePast, CancellationToken cancellationToken = default);
    Task<ServiceResult<GenerateScheduleResult>> GenerateAsync(int providerId, GenerateScheduleCommand command, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> AddSingleAsync(int providerId, AddSingleSlotCommand command, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeleteAsync(int providerId, int slotId, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> ClearDayAsync(int providerId, DateTime date, CancellationToken cancellationToken = default);
}

