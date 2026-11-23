using System;

namespace Zyara.Services.Features.Shared;

public static class EgyptTime
{
    private static readonly TimeZoneInfo TimeZone = ResolveTimeZone();

    public static DateTime Now(TimeProvider? timeProvider = null) =>
        TimeZoneInfo.ConvertTimeFromUtc((timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime, TimeZone);

    public static DateTime Today(TimeProvider? timeProvider = null) =>
        Now(timeProvider).Date;

    public static DateTime ConvertUtcToEgypt(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc 
            ? utcDateTime 
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
            }
            catch (TimeZoneNotFoundException ex)
            {
                throw new InvalidOperationException("Neither the Egypt Standard Time nor Africa/Cairo time zone is available.", ex);
            }
        }
    }
}
