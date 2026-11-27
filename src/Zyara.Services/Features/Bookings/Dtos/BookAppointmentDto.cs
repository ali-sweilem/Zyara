using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Zyara.Services.Features.Patients.Dtos;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Services.Features.Bookings.Dtos;

public sealed class BookAppointmentDto
{
    public int PatientId { get; set; }
    public int ProviderId { get; set; }
    public int TimeSlotId { get; set; }
    public int ProviderServiceId { get; set; }
    public string ProviderFullName { get; set; } = string.Empty;
    public string ProviderSpecialty { get; set; } = string.Empty;
    public string? ProviderPhotoUrl { get; set; }
    public string ProviderBaseCity { get; set; } = string.Empty;
    public IReadOnlyList<BookingSlotDto> AvailableSlots { get; set; } = [];
    public IReadOnlyList<ProviderServiceItemDto> ProviderServices { get; set; } = [];

    [Required(ErrorMessage = "Please select a time slot.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid time slot.")]
    public int SelectedTimeSlotId { get; set; }

    [Required(ErrorMessage = "Please select a service.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid service.")]
    public int SelectedProviderServiceId { get; set; }

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    // Patient Addresses for Visit
    public IReadOnlyList<PatientAddressItemDto> PatientAddresses { get; set; } = [];
    public int? SelectedAddressId { get; set; }
    public string? CustomAddress { get; set; }

    // ── Care recipient (who the visit is for) ──
    /// <summary>Loaded from DB for the booking form; not posted back.</summary>
    public IReadOnlyList<CareRecipientDto> CareRecipients { get; set; } = [];

    [Required(ErrorMessage = "Please choose who this visit is for.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please choose who this visit is for.")]
    public int SelectedCareRecipientId { get; set; }

    // Payment Selection
    public string PaymentMethod { get; set; } = "Cash"; // "Cash" or "Card"

    // Card Details (when PaymentMethod == "Card")
    [Display(Name = "Cardholder Name")]
    public string? CardHolderName { get; set; }

    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [Display(Name = "Expiry Date (MM/YY)")]
    public string? CardExpiry { get; set; }

    [Display(Name = "CVV")]
    public string? CardCvv { get; set; }
}

