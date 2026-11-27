using Zyara.Services.Common;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Services.Features.Providers.Interfaces;

/// <summary>
/// Fixed weekly working times. The provider adds "every Sunday 10:00" once and the real TimeSlots
/// are created for the next 4 weeks and extended automatically. providerId always comes from ICurrentUser.
/// </summary>
public interface IWeeklyScheduleService
{
    Task<List<WeeklySlotDto>> GetAsync(int providerId, CancellationToken cancellationToken = default);

    /// <summary>Saves the weekly time and immediately creates its slots for the next 4 weeks.</summary>
    Task<ServiceResult> AddAsync(int providerId, CreateWeeklySlotDto dto, CancellationToken cancellationToken = default);

    /// <summary>Splits [startTime, endTime) into 2-hour weekly slots across all selected days.</summary>
    Task<ServiceResult<int>> AddShiftRangeAsync(
        int providerId,
        IReadOnlyCollection<DayOfWeek> days,
        TimeSpan startTime,
        TimeSpan endTime,
        CancellationToken cancellationToken = default);

    /// <summary>Stops the repetition and removes its FUTURE slots that are still Available (booked / locked ones stay).</summary>
    Task<ServiceResult> RemoveAsync(int providerId, int weeklySlotId, CancellationToken cancellationToken = default);

    /// <summary>Creates the missing slots of the next 4 weeks. One provider, or every provider when null. Returns the number created.</summary>
    Task<int> GenerateAsync(int? providerId = null, CancellationToken cancellationToken = default);
}
