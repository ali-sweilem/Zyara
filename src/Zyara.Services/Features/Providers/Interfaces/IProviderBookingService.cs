using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Services.Features.Providers.Interfaces;

public interface IProviderBookingService
{
    Task<IReadOnlyList<ProviderRequestCardDto>> GetRequestsAsync(int providerId, CancellationToken cancellationToken = default);

    Task<PagedResult<ProviderVisitItemDto>> GetVisitsAsync(
        int providerId,
        string group,
        string? statusFilter,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ProviderVisitDetailsDto?> GetVisitDetailsAsync(int providerId, int bookingId, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> AcceptAsync(int providerId, int bookingId, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> RejectAsync(int providerId, int bookingId, string reason, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> StartAsync(int providerId, int bookingId, string? otp, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> CompleteAsync(int providerId, int bookingId, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> CancelAsync(int providerId, int bookingId, string reason, CancellationToken cancellationToken = default);
}

