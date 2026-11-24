using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services.Features.Providers;

public sealed class WeeklyScheduleService : IWeeklyScheduleService
{
    private const int SlotMinutes = 120;   // fixed 2-hour arrival window (same rule as the manual "Add Time Slot")
    private const int HorizonDays = 28;    // slots are kept ready 4 weeks ahead

    private readonly IRepository<WeeklySlotTemplate> _templates;
    private readonly IRepository<TimeSlot> _slots;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public WeeklyScheduleService(
        IRepository<WeeklySlotTemplate> templates,
        IRepository<TimeSlot> slots,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _templates = templates;
        _slots = slots;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<List<WeeklySlotDto>> GetAsync(int providerId, CancellationToken cancellationToken = default)
    {
        var rows = await _templates.Query()
            .AsNoTracking()
            .Where(t => t.ProviderId == providerId)
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(t => ((int)t.DayOfWeek + 1) % 7)   // Egypt week: Saturday first
            .ThenBy(t => t.StartTime)
            .Select(t => new WeeklySlotDto
            {
                Id = t.Id,
                DayOfWeek = t.DayOfWeek,
                StartTime = t.StartTime,
                EndTime = t.StartTime + TimeSpan.FromMinutes(t.SlotDurationMinutes)
            })
            .ToList();
    }

    public async Task<ServiceResult> AddAsync(int providerId, CreateWeeklySlotDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.StartTime is null)
        {
            return ServiceResult.Fail("Start time is required.");
        }

        var start = dto.StartTime.Value;
        var end = start + TimeSpan.FromMinutes(SlotMinutes);

        if (start < TimeSpan.Zero || end > TimeSpan.FromHours(24))
        {
            return ServiceResult.Fail("This time exceeds the 24-hour day boundary.");
        }

        var sameDay = await _templates.Query()
            .AsNoTracking()
            .Where(t => t.ProviderId == providerId && t.DayOfWeek == dto.DayOfWeek)
            .ToListAsync(cancellationToken);

        if (sameDay.Any(t => t.StartTime < end && t.StartTime + TimeSpan.FromMinutes(t.SlotDurationMinutes) > start))
        {
            return ServiceResult.Fail("This time overlaps with another weekly time on the same day.");
        }

        _templates.Add(new WeeklySlotTemplate
        {
            ProviderId = providerId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = start,
            SlotDurationMinutes = SlotMinutes,
            GeneratedThrough = EgyptTime.Now(_timeProvider).Date.AddDays(-1)   // nothing generated yet
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await GenerateAsync(providerId, cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveAsync(int providerId, int weeklySlotId, CancellationToken cancellationToken = default)
    {
        // ProviderId in the filter: a provider can only remove his own weekly times.
        var template = await _templates.FirstOrDefaultAsync(
            t => t.Id == weeklySlotId && t.ProviderId == providerId, cancellationToken);

        if (template is null)
        {
            return ServiceResult.Fail("Weekly time not found.");
        }

        var today = EgyptTime.Now(_timeProvider).Date;

        var futureFree = await _slots.Query()
            .Where(s => s.ProviderId == providerId
                        && s.Status == TimeSlotStatus.Available
                        && s.Date >= today
                        && s.StartTime == template.StartTime)
            .ToListAsync(cancellationToken);

        foreach (var slot in futureFree.Where(s => s.Date.DayOfWeek == template.DayOfWeek))
        {
            _slots.Remove(slot);
        }

        _templates.Remove(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<int> GenerateAsync(int? providerId = null, CancellationToken cancellationToken = default)
    {
        var now = EgyptTime.Now(_timeProvider);
        var today = now.Date;
        var horizonEnd = today.AddDays(HorizonDays - 1);

        var query = _templates.Query()
            .Where(t => t.GeneratedThrough < horizonEnd && t.Provider!.IsActive);

        if (providerId is int id)
        {
            query = query.Where(t => t.ProviderId == id);
        }

        var templates = await query.ToListAsync(cancellationToken);   // tracked: GeneratedThrough is saved below
        if (templates.Count == 0)
        {
            return 0;
        }

        // Slots that already exist in the window, so a generated slot never overlaps another one.
        var providerIds = templates.Select(t => t.ProviderId).Distinct().ToList();
        var occupied = (await _slots.Query()
                .AsNoTracking()
                .Where(s => providerIds.Contains(s.ProviderId) && s.Date >= today && s.Date <= horizonEnd)
                .Select(s => new { s.ProviderId, s.Date, s.StartTime, s.EndTime })
                .ToListAsync(cancellationToken))
            .Select(s => new Occupied(s.ProviderId, s.Date.Date, s.StartTime, s.EndTime))
            .ToList();

        var created = 0;

        foreach (var template in templates)
        {
            var next = template.GeneratedThrough.Date.AddDays(1);
            var from = next > today ? next : today;
            var duration = TimeSpan.FromMinutes(template.SlotDurationMinutes);
            var end = template.StartTime + duration;

            for (var date = from; date <= horizonEnd; date = date.AddDays(1))
            {
                if (date.DayOfWeek != template.DayOfWeek) continue;
                if (date == today && template.StartTime <= now.TimeOfDay) continue;   // that time already started today

                var clash = occupied.Any(o => o.ProviderId == template.ProviderId
                                              && o.Date == date
                                              && o.Start < end
                                              && o.End > template.StartTime);
                if (clash) continue;

                _slots.Add(new TimeSlot
                {
                    ProviderId = template.ProviderId,
                    Date = date,
                    StartTime = template.StartTime,
                    EndTime = end,
                    SlotDurationMinutes = template.SlotDurationMinutes,
                    Status = TimeSlotStatus.Available
                });
                occupied.Add(new Occupied(template.ProviderId, date, template.StartTime, end));
                created++;
            }

            template.GeneratedThrough = horizonEnd;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    private sealed record Occupied(int ProviderId, DateTime Date, TimeSpan Start, TimeSpan End);
}
