using System;

namespace Zyara.Services.Features.Bookings.Dtos;

/// <summary>
/// Carries the patient's booking selections submitted from the Book view.
/// Only fields the patient is allowed to set are included — provider ID comes from
/// the route, patient ID is read from the authenticated principal, never the form.
/// </summary>
public sealed class BookAppointmentDto
{
    /// <summary>Provider whose slot is being booked (set from route, not form).</summary>
    public int ProviderId { get; set; }

    /// <summary>Patient submitting the booking (set from claims/session, not form).</summary>
    public int PatientId { get; set; }

    /// <summary>The specific TimeSlot the patient selected.</summary>
    public int TimeSlotId { get; set; }

    /// <summary>The ProviderService the patient chose (determines fee and service type).</summary>
    public int ProviderServiceId { get; set; }

    /// <summary>Optional free-text notes (max 500 chars).</summary>
    public string? Notes { get; set; }
}

