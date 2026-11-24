namespace Zyara.Domain.Rules;

/// <summary>
/// Pure time rules for automatic booking clean-up (no database, no clock: "now" is passed in).
/// All times are Egypt local time, the same as TimeSlot.Date + TimeSlot.StartTime.
/// </summary>
public static class BookingTimeRules
{
    /// <summary>A visit that has not been started this long after the window opens is a no-show (README: 30 minutes).</summary>
    public static readonly TimeSpan NoShowGrace = TimeSpan.FromMinutes(30);

    /// <summary>A request the provider never answered expires when the appointment window starts.</summary>
    public static bool IsExpired(DateTime slotStart, DateTime now) => slotStart <= now;

    /// <summary>A confirmed booking that was never started becomes NoShow 30 minutes after the window starts.</summary>
    public static bool IsNoShow(DateTime slotStart, DateTime now) => slotStart + NoShowGrace <= now;
}
