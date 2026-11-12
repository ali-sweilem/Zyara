using Zyara.Services.Features.Bookings.Dtos;

namespace Zyara.Services.Features.Bookings.Interfaces;

public interface ISlotService
{
    Task<List<SlotDto>> GetAvailableSlotsAsync(int providerId, DateTime? date = null, CancellationToken cancellationToken = default);
}
