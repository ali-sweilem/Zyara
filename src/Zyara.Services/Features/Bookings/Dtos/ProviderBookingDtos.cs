using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Bookings.Dtos;

public sealed class ProviderBookingItemDto
{
    public int Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public bool IsHomeVisit { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public BookingStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }

    /// <summary>The cancellation commission fixed for this booking.</summary>
    public decimal Commission { get; set; }

    /// <summary>What cancelling THIS booking right now would cost the provider (0 unless it is the day of the visit).</summary>
    public decimal CancelPenaltyNow { get; set; }

    /// <summary>An earlier cancellation penalty this patient still owes; the provider collects it with this visit.</summary>
    public decimal PatientOutstandingPenalty { get; set; }
}

/// <summary>The 'Booking requests' page: what needs the provider's action.</summary>
public sealed class ProviderBookingsPageDto
{
    /// <summary>Waiting for this provider to accept or reject.</summary>
    public List<ProviderBookingItemDto> Pending { get; set; } = new();

    /// <summary>Confirmed or started: the provider still has work to do.</summary>
    public List<ProviderBookingItemDto> Active { get; set; } = new();

    /// <summary>Same-day cancellations this week and whether new bookings are paused.</summary>
    public ProviderCancellationStatusDto CancellationStatus { get; set; } = new();

}
