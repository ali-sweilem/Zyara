using System;
using System.Threading;
using System.Threading.Tasks;
using Zyara.Domain.Entities;
using Zyara.Services.Common;

namespace Zyara.Services.Features.Bookings;

public interface IBookingTransitions
{
    Task<ServiceResult<bool>> AcceptAsync(Booking booking, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> RejectAsync(Booking booking, string reason, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> ExpireAsync(Booking booking, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> CancelByProviderAsync(Booking booking, string reason, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> StartAsync(Booking booking, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> CompleteAsync(Booking booking, DateTime utcNow, CancellationToken cancellationToken = default);
}

