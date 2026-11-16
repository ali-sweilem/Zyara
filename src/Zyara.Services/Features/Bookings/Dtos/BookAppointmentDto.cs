using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
}

