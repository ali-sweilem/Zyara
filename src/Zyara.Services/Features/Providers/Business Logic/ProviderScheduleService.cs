using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Repositories.Data;
using Zyara.Services.Common;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services.Features.Providers;

public class ProviderScheduleService : IProviderScheduleService
{
    private readonly ZyaraDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ProviderScheduleService(ZyaraDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<ProviderScheduleViewDto> GetScheduleAsync(
        int providerId,
        bool includePast,
        CancellationToken cancellationToken = default)
    {
        var egyptNow = EgyptTime.Now(_timeProvider);
        var today = egyptNow.Date;

        var query = _db.TimeSlots
            .AsNoTracking()
            .Where(ts => ts.ProviderId == providerId);

        if (!includePast)
        {
            query = query.Where(ts => ts.Date > today || (ts.Date == today && ts.EndTime > egyptNow.TimeOfDay));
        }

        var slots = await query
            .OrderBy(ts => ts.Date)
            .ThenBy(ts => ts.StartTime)
            .ToListAsync(cancellationToken);

        var availableCount = slots.Count(s => s.Status == TimeSlotStatus.Available);
        var pendingApprovalCount = slots.Count(s => s.Status == TimeSlotStatus.Locked);
        var bookedCount = slots.Count(s => s.Status == TimeSlotStatus.Booked);

        var groups = slots
            .GroupBy(s => s.Date.Date)
            .Select(g => new ProviderScheduleDayGroupDto
            {
                Date = g.Key,
                DayLabel = SlotFormatting.FormatDayLabel(g.Key, egyptNow),
                Slots = g.Select(s => new ProviderTimeSlotItemDto
                {
                    Id = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    FormattedWindow = SlotFormatting.FormatWindow(s.StartTime, s.EndTime),
                    Status = s.Status
                }).ToList()
            })
            .ToList();

        return new ProviderScheduleViewDto
        {
            AvailableCount = availableCount,
            PendingApprovalCount = pendingApprovalCount,
            BookedCount = bookedCount,
            DayGroups = groups,
            IncludePast = includePast
        };
    }

    public async Task<ServiceResult<GenerateScheduleResult>> GenerateAsync(
        int providerId,
        GenerateScheduleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.FromHour < 0 || command.FromHour >= 24)
            return ServiceResult<GenerateScheduleResult>.Fail("From hour must be between 0 and 23.");
        if (command.ToHour <= command.FromHour || command.ToHour > 24)
            return ServiceResult<GenerateScheduleResult>.Fail("To hour must be greater than From hour and up to 24.");
        if (command.RepeatWeeks is not (1 or 2 or 4 or 8))
            return ServiceResult<GenerateScheduleResult>.Fail("Repeat weeks must be 1, 2, 4, or 8 weeks.");
        if (command.DaysOfWeek == null || command.DaysOfWeek.Count == 0)
            return ServiceResult<GenerateScheduleResult>.Fail("Please select at least one day of the week.");

        var egyptNow = EgyptTime.Now(_timeProvider);
        var today = egyptNow.Date;
        var horizonDate = today.AddDays(BookingRules.BookingHorizonDays);

        var windowDurationMinutes = BookingRules.SlotWindowMinutes;
        var windowDuration = TimeSpan.FromMinutes(windowDurationMinutes);

        var fromTime = TimeSpan.FromHours(command.FromHour);
        var toTime = TimeSpan.FromHours(command.ToHour);

        // Precompute windows within [FromHour, ToHour)
        var windowTimes = new List<(TimeSpan Start, TimeSpan End)>();
        var currentStart = fromTime;
        int droppedPartial = 0;

        while (currentStart + windowDuration <= toTime)
        {
            windowTimes.Add((currentStart, currentStart + windowDuration));
            currentStart += windowDuration;
        }

        if (currentStart < toTime)
        {
            droppedPartial++;
        }

        if (windowTimes.Count == 0)
        {
            return ServiceResult<GenerateScheduleResult>.Fail(
                $"Working hours {command.FromHour}:00 to {command.ToHour}:00 cannot fit a full {windowDurationMinutes}-minute window.");
        }

        // Query existing slots for provider in the horizon to detect overlaps
        var existingSlots = await _db.TimeSlots
            .Where(ts => ts.ProviderId == providerId && ts.Date >= today && ts.Date <= horizonDate)
            .Select(ts => new { ts.Date, ts.StartTime, ts.EndTime })
            .ToListAsync(cancellationToken);

        var startDate = command.StartDate.Date < today ? today : command.StartDate.Date;
        var totalDays = command.RepeatWeeks * 7;
        var targetDates = new List<DateTime>();

        for (int i = 0; i < totalDays; i++)
        {
            var d = startDate.AddDays(i);
            if (command.DaysOfWeek.Contains(d.DayOfWeek))
            {
                targetDates.Add(d);
            }
        }

        int created = 0;
        int skippedOverlap = 0;
        int skippedPast = 0;
        int skippedBeyondHorizon = 0;
        var newSlots = new List<TimeSlot>();

        foreach (var date in targetDates)
        {
            if (date > horizonDate)
            {
                skippedBeyondHorizon += windowTimes.Count;
                continue;
            }

            foreach (var (wStart, wEnd) in windowTimes)
            {
                if (newSlots.Count + created >= 200)
                {
                    break;
                }

                // Check if in past
                var slotStartEgypt = date + wStart;
                if (slotStartEgypt <= egyptNow)
                {
                    skippedPast++;
                    continue;
                }

                // Check overlap with existing DB slots or newly staged slots
                bool hasOverlap = existingSlots.Any(e => e.Date.Date == date && e.StartTime < wEnd && e.EndTime > wStart) ||
                                  newSlots.Any(n => n.Date.Date == date && n.StartTime < wEnd && n.EndTime > wStart);

                if (hasOverlap)
                {
                    skippedOverlap++;
                    continue;
                }

                newSlots.Add(new TimeSlot
                {
                    ProviderId = providerId,
                    Date = date,
                    StartTime = wStart,
                    EndTime = wEnd,
                    SlotDurationMinutes = windowDurationMinutes,
                    Status = TimeSlotStatus.Available
                });
                created++;
            }

            if (newSlots.Count >= 200) break;
        }

        if (newSlots.Count > 0)
        {
            _db.TimeSlots.AddRange(newSlots);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<GenerateScheduleResult>.Ok(new GenerateScheduleResult
        {
            Created = created,
            SkippedOverlap = skippedOverlap,
            SkippedPast = skippedPast,
            SkippedBeyondHorizon = skippedBeyondHorizon,
            DroppedPartial = droppedPartial
        });
    }

    public async Task<ServiceResult<bool>> AddSingleAsync(
        int providerId,
        AddSingleSlotCommand command,
        CancellationToken cancellationToken = default)
    {
        var egyptNow = EgyptTime.Now(_timeProvider);
        var today = egyptNow.Date;
        var horizonDate = today.AddDays(BookingRules.BookingHorizonDays);

        var slotDate = command.Date.Date;
        if (slotDate < today)
        {
            return ServiceResult<bool>.Fail("Cannot schedule windows in the past.");
        }
        if (slotDate > horizonDate)
        {
            return ServiceResult<bool>.Fail($"Windows cannot be scheduled beyond the {BookingRules.BookingHorizonDays}-day horizon.");
        }

        var durationMinutes = BookingRules.SlotWindowMinutes;
        var startTime = command.StartTime;
        var endTime = startTime.Add(TimeSpan.FromMinutes(durationMinutes));

        if (endTime > TimeSpan.FromHours(24) || endTime <= startTime)
        {
            return ServiceResult<bool>.Fail("Window extends beyond the 24-hour day boundary.");
        }

        var slotStartEgypt = slotDate + startTime;
        if (slotStartEgypt <= egyptNow)
        {
            return ServiceResult<bool>.Fail("The selected start time is in the past.");
        }

        // Check overlap
        var hasOverlap = await _db.TimeSlots
            .AnyAsync(ts => ts.ProviderId == providerId &&
                            ts.Date == slotDate &&
                            ts.StartTime < endTime &&
                            ts.EndTime > startTime, cancellationToken);

        if (hasOverlap)
        {
            return ServiceResult<bool>.Fail("This window overlaps with an existing time slot on this date.");
        }

        var timeSlot = new TimeSlot
        {
            ProviderId = providerId,
            Date = slotDate,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = durationMinutes,
            Status = TimeSlotStatus.Available
        };

        _db.TimeSlots.Add(timeSlot);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(
        int providerId,
        int slotId,
        CancellationToken cancellationToken = default)
    {
        var slot = await _db.TimeSlots
            .FirstOrDefaultAsync(ts => ts.Id == slotId && ts.ProviderId == providerId, cancellationToken);

        if (slot is null)
        {
            return ServiceResult<bool>.Fail("Slot not found.");
        }

        if (slot.Status != TimeSlotStatus.Available)
        {
            return ServiceResult<bool>.Fail("Only Available windows can be deleted. Locked or Booked windows cannot be removed.");
        }

        _db.TimeSlots.Remove(slot);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<int>> ClearDayAsync(
        int providerId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var targetDate = date.Date;

        var availableSlots = await _db.TimeSlots
            .Where(ts => ts.ProviderId == providerId &&
                         ts.Date == targetDate &&
                         ts.Status == TimeSlotStatus.Available)
            .ToListAsync(cancellationToken);

        if (availableSlots.Count == 0)
        {
            return ServiceResult<int>.Ok(0);
        }

        _db.TimeSlots.RemoveRange(availableSlots);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<int>.Ok(availableSlots.Count);
    }
}

