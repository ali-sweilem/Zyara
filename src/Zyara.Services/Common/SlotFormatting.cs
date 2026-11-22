using System;
using System.Globalization;

namespace Zyara.Services.Common;

public static class SlotFormatting
{
    public static string FormatWindow(TimeSpan startTime, TimeSpan endTime)
    {
        // Use a fixed reference date (not DateTime.Today) just to format TimeSpan → 12-hour string
        var refDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var startFormatted = refDate.Add(startTime).ToString("hh:mm tt", CultureInfo.InvariantCulture);
        var endFormatted = refDate.Add(endTime).ToString("hh:mm tt", CultureInfo.InvariantCulture);
        return $"{startFormatted} – {endFormatted}";
    }

    public static string FormatWindow(TimeSpan startTime, int durationMinutes = 120)
    {
        return FormatWindow(startTime, startTime.Add(TimeSpan.FromMinutes(durationMinutes)));
    }

    public static string FormatDayLabel(DateTime date, DateTime egyptNow)
    {
        var targetDate = date.Date;
        var today = egyptNow.Date;

        if (targetDate == today)
        {
            return "Today";
        }
        if (targetDate == today.AddDays(1))
        {
            return "Tomorrow";
        }

        return targetDate.ToString("ddd d MMM", CultureInfo.InvariantCulture);
    }

    public static string GetPartOfDay(TimeSpan startTime)
    {
        if (startTime < TimeSpan.FromHours(12))
        {
            return "Morning";
        }
        if (startTime < TimeSpan.FromHours(17))
        {
            return "Afternoon";
        }
        return "Evening";
    }
}

