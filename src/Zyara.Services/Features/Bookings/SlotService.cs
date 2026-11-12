using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;

namespace Zyara.Services.Features.Bookings;

public sealed class SlotService : ISlotService
{
    private readonly ZyaraDbContext _db;

    public SlotService(ZyaraDbContext db)
    {
        _db = db;
    }

    public async Task<List<SlotDto>> GetAvailableSlotsAsync(int providerId, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var query = _db.TimeSlots
            .AsNoTracking()
            .Where(s => s.ProviderId == providerId && s.Status == TimeSlotStatus.Available);

        if (date.HasValue)
        {
            var day = date.Value.Date;
            query = query.Where(s => s.Date == day);
        }
        else
        {
            var today = DateTime.Today;
            query = query.Where(s => s.Date >= today);
        }

        return await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => new SlotDto
            {
                Id = s.Id,
                Date = s.Date,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .ToListAsync(cancellationToken);
    }
}
