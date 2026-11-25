namespace Zyara.Services.Features.Bookings.Dtos;

/// <summary>What happened when a booking was cancelled (the text is shown to the person who cancelled).</summary>
public sealed class CancellationResultDto
{
    /// <summary>What the canceller was charged. 0 = free cancellation.</summary>
    public decimal Fee { get; init; }

    public string Message { get; init; } = string.Empty;
}

/// <summary>A provider's same-day cancellations this week and whether new bookings are paused.</summary>
public sealed class ProviderCancellationStatusDto
{
    public int CountThisWeek { get; init; }
    public int Limit { get; init; } = Zyara.Domain.Rules.CancellationRules.ProviderSuspendAfter;
    public bool IsSuspended { get; init; }

    /// <summary>Egypt local time. Only set while suspended.</summary>
    public DateTime? SuspendedUntil { get; init; }
}
