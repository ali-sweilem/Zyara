using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Bookings.Dtos;

public sealed class CreateBookingDto
{
    public int ProviderId { get; set; }
    public int ServiceId { get; set; }
    public int SlotId { get; set; }

    [Required]
    public string PatientName { get; set; } = string.Empty;
    public int PatientAge { get; set; }
    [Required]
    public string PatientPhone { get; set; } = string.Empty;
    public string? PatientNote { get; set; }

    // Home visits only
    public string? Street { get; set; }
    public string? City { get; set; }

    public string AddressLabel { get; set; } = "Home";
    public string District { get; set; } = string.Empty;
}
