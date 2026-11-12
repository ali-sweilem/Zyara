
using System.ComponentModel.DataAnnotations;
using Zyara.Services.Features.Bookings.Dtos;

namespace Zyara.Web.Models;

public class BookingCreateViewModel
{
    public int ProviderId { get; set; }
    public int ServiceId { get; set; }

    public int SelectedSlotId { get; set; }

    public List<SlotDto> Slots { get; set; } = new();

    [Required(ErrorMessage = "Patient name is required.")]
    public string PatientName { get; set; } = string.Empty;

    [Range(1, 120, ErrorMessage = "Please enter a valid age.")]
    public int PatientAge { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    public string PatientPhone { get; set; } = string.Empty;

    public string? PatientNote { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string AddressLabel { get; set; } = "Home";
    public string District { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}
