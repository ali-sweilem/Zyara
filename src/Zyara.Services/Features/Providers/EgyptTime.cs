using System;

namespace Zyara.Services.Features.Providers;

internal static class EgyptTime
{
    private static readonly TimeZoneInfo TimeZone = ResolveTimeZone();

    public static DateTime Now(TimeProvider timeProvider) =>
        TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, TimeZone);

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
