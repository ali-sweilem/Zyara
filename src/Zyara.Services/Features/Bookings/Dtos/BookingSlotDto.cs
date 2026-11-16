using System;

namespace Zyara.Services.Features.Bookings.Dtos;

/// <summary>
/// A single available time slot returned to the Book view.
/// </summary>
public sealed class BookingSlotDto
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }

    /// <summary>Formatted label shown in the slot selector dropdown.</summary>
    public string Label => $"{Date:ddd d MMM} — {StartTime:hh\\:mm} to {EndTime:hh\\:mm}";
}

