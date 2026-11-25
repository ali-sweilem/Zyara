namespace Zyara.Domain.Rules;

/// <summary>
/// Pure cancellation rules (no database, no clock: "now" is passed in).
/// All times are Egypt local time, the same as TimeSlot.Date + TimeSlot.StartTime.
/// </summary>
public static class CancellationRules
{
    /// <summary>A patient who cancels a CONFIRMED booking less than this long before the visit pays the commission.</summary>
    public static readonly TimeSpan LateCancelWindow = TimeSpan.FromHours(1);

    /// <summary>A provider with this many same-day cancellations in a week stops receiving new bookings.</summary>
    public const int ProviderSuspendAfter = 2;

    /// <summary>Less than one hour left before the visit window opens (or it already opened).</summary>
    public static bool IsLatePatientCancel(DateTime slotStart, DateTime now) => now >= slotStart - LateCancelWindow;

    /// <summary>The provider cancels on the day of the visit.</summary>
    public static bool IsSameDay(DateTime slotStart, DateTime now) => slotStart.Date == now.Date;

    /// <summary>The Saturday (00:00) that starts the week of the given date. The week runs Saturday to Friday.</summary>
    public static DateTime WeekStart(DateTime date)
    {
        var daysSinceSaturday = ((int)date.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        return date.Date.AddDays(-daysSinceSaturday);
    }

    /// <summary>The stored count only counts while its week is the current week; a new week starts again from zero.</summary>
    public static int EffectiveWeeklyCount(int storedCount, DateTime? storedWeekStart, DateTime now) =>
        storedWeekStart.HasValue && storedWeekStart.Value.Date == WeekStart(now) ? storedCount : 0;
}
