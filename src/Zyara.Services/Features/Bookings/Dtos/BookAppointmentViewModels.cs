using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Bookings.Dtos;

public class BookAppointmentViewModel
{
    public int ProviderId { get; set; }
    public string ProviderFullName { get; set; } = string.Empty;
    public string? ProviderSpecialty { get; set; }
    public string? ProviderPhotoUrl { get; set; }
    public string? ProviderBaseCity { get; set; }

    public decimal PlatformServiceFee { get; set; }

    public IReadOnlyList<BookAppointmentServiceItemDto> Services { get; set; } = [];
    public IReadOnlyList<BookAppointmentDayDto> Days { get; set; } = [];
    public IReadOnlyList<AddressDto> SavedAddresses { get; set; } = [];

    public int? PreselectedServiceId { get; set; }
    public int? PreselectedSlotId { get; set; }
    public string? PreselectedDay { get; set; }

    public BookAppointmentPostModel Form { get; set; } = new();
}

public class BookAppointmentServiceItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Fee { get; set; }
    public ServiceType Type { get; set; }
}

public class BookAppointmentDayDto
{
    public DateTime Date { get; set; }
    public string DateKey => Date.ToString("yyyy-MM-dd");
    public string DayLabel { get; set; } = string.Empty;
    public string WeekdayName { get; set; } = string.Empty;
    public string DayNumber { get; set; } = string.Empty;
    public string MonthName { get; set; } = string.Empty;
    public int WindowsCount => Windows.Count;
    public List<BookAppointmentWindowDto> Windows { get; set; } = [];
}

public class BookAppointmentWindowDto
{
    public int Id { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string FormattedWindow { get; set; } = string.Empty;
    public string PartOfDay { get; set; } = string.Empty; // Morning, Afternoon, Evening
}

public class BookAppointmentPostModel
{
    [Required]
    public int ProviderId { get; set; }

    [Required(ErrorMessage = "Please select a service.")]
    public int SelectedProviderServiceId { get; set; }

    [Required(ErrorMessage = "Please select an arrival window.")]
    public int SelectedTimeSlotId { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnArrival;

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    // Address selection
    public int? AddressId { get; set; }
    public string? Label { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? StreetAndBuilding { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool SaveNewAddress { get; set; } = true;
}

public class CardPayInputModel
{
    [Required]
    public int BookingId { get; set; }

    [Required(ErrorMessage = "Cardholder name is required.")]
    public string CardHolderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Card number is required.")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Expiry month is required.")]
    [RegularExpression(@"^(0[1-9]|1[0-2])$", ErrorMessage = "Expiry month must be MM (01-12).")]
    public string ExpiryMonth { get; set; } = string.Empty;

    [Required(ErrorMessage = "Expiry year is required.")]
    [RegularExpression(@"^([0-9]{2}|20[0-9]{2})$", ErrorMessage = "Expiry year must be YY or YYYY.")]
    public string ExpiryYear { get; set; } = string.Empty;

    [Required(ErrorMessage = "CVV is required.")]
    [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
    public string Cvv { get; set; } = string.Empty;
}

