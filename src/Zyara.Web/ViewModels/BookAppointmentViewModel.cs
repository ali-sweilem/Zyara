using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Web.ViewModels;

/// <summary>
/// View model for the Book Appointment page (GET and POST).
/// Only patient-settable fields carry validation attributes.
/// Provider identity and pricing are read-only display fields populated server-side.
/// </summary>
public sealed class BookAppointmentViewModel
{
    // ── Provider summary (display only — never trusted from form) ──────────────
    public int ProviderId { get; set; }
    public string ProviderFullName { get; set; } = string.Empty;
    public string ProviderSpecialty { get; set; } = string.Empty;
    public string? ProviderPhotoUrl { get; set; }
    public string ProviderBaseCity { get; set; } = string.Empty;

    // ── Available slots (populated on GET; repopulated on failed POST) ─────────
    public IReadOnlyList<BookingSlotDto> AvailableSlots { get; set; } = [];

    // ── Services offered by this provider ─────────────────────────────────────
    public IReadOnlyList<ProviderServiceItemDto> ProviderServices { get; set; } = [];

    // ── Patient choices (the only fields bound on POST) ────────────────────────

    [Required(ErrorMessage = "Please select a time slot.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid time slot.")]
    public int SelectedTimeSlotId { get; set; }

    [Required(ErrorMessage = "Please select a service.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid service.")]
    public int SelectedProviderServiceId { get; set; }

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}

