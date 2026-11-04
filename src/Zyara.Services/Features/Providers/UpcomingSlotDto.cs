using System;

namespace Zyara.Services.Features.Providers;

public sealed class UpcomingSlotDto
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
}
