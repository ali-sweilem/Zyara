using System;
using System.Linq;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Rules;

public static class TimeSlotQueryExtensions
{
    public static IQueryable<TimeSlot> BookableSlots(this IQueryable<TimeSlot> query, DateTime egyptNow)
    {
        var minStartDateTime = egyptNow.AddHours(BookingRules.MinLeadTimeHours);
        var minDate = minStartDateTime.Date;
        var minTime = minStartDateTime.TimeOfDay;
        var maxDate = egyptNow.Date.AddDays(BookingRules.BookingHorizonDays);

        return query.Where(ts =>
            ts.Status == TimeSlotStatus.Available &&
            ts.Date <= maxDate &&
            (ts.Date > minDate || (ts.Date == minDate && ts.StartTime >= minTime)));
    }

    public static bool IsBookable(this TimeSlot ts, DateTime egyptNow)
    {
        var minStartDateTime = egyptNow.AddHours(BookingRules.MinLeadTimeHours);
        var maxDate = egyptNow.Date.AddDays(BookingRules.BookingHorizonDays);
        var slotStart = ts.Date.Date + ts.StartTime;

        return ts.Status == TimeSlotStatus.Available &&
               ts.Date.Date <= maxDate &&
               slotStart >= minStartDateTime;
    }
}

