using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Bookings.Dtos;

public sealed class ProviderBookingItemDto
{
    public int Id { get; set; }

    // ── Privacy rule: name and phone hidden until booking is Confirmed/Started ──
    /// <summary>Null or "Patient (pending)" while status is PendingProviderApproval.</summary>
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }

    // ── Medical details (always visible to provider) ──
    public Gender? CareRecipientGender { get; set; }
    public DateOnly? CareRecipientDateOfBirth { get; set; }
    public BloodType? CareRecipientBloodType { get; set; }
    public string? CareRecipientChronicConditions { get; set; }
    public string? CareRecipientAllergies { get; set; }
    public string? CareRecipientCurrentMedications { get; set; }

    public string BloodTypeDisplay => CareRecipientBloodType?.ToDisplayString() ?? string.Empty;

    public int? PatientAge
    {
        get
        {
            if (!CareRecipientDateOfBirth.HasValue) return null;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - CareRecipientDateOfBirth.Value.Year;
            if (CareRecipientDateOfBirth.Value > today.AddYears(-age)) age--;
            return age;
        }
    }

    public string ServiceName { get; set; } = string.Empty;
    public bool IsHomeVisit { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public BookingStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
}

/// <summary>The 'Booking requests' page: what needs the provider's action.</summary>
public sealed class ProviderBookingsPageDto
{
    /// <summary>Waiting for this provider to accept or reject.</summary>
    public List<ProviderBookingItemDto> Pending { get; set; } = new();

    /// <summary>Confirmed or started: the provider still has work to do.</summary>
    public List<ProviderBookingItemDto> Active { get; set; } = new();

}
