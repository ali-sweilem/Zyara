using System;
using System.Collections.Generic;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Providers.Dtos;

public class GenerateScheduleCommand
{
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];
    public int FromHour { get; set; } = 9;
    public int ToHour { get; set; } = 17;
    public int RepeatWeeks { get; set; } = 1;
    public DateTime StartDate { get; set; }
}

public class GenerateScheduleResult
{
    public int Created { get; set; }
    public int SkippedOverlap { get; set; }
    public int SkippedPast { get; set; }
    public int SkippedBeyondHorizon { get; set; }
    public int DroppedPartial { get; set; }
}

public class AddSingleSlotCommand
{
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
}

public class ProviderScheduleViewDto
{
    public int AvailableCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int BookedCount { get; set; }
    public List<ProviderScheduleDayGroupDto> DayGroups { get; set; } = [];
    public bool IncludePast { get; set; }
}

public class ProviderScheduleDayGroupDto
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = string.Empty;
    public List<ProviderTimeSlotItemDto> Slots { get; set; } = [];
}

public class ProviderTimeSlotItemDto
{
    public int Id { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string FormattedWindow { get; set; } = string.Empty;
    public TimeSlotStatus Status { get; set; }
    public bool CanDelete => Status == TimeSlotStatus.Available;
}

