using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Bookings.Dtos;

public sealed class CreateBookingCommand
{
    public int PatientId { get; set; }
    public int ProviderId { get; set; }
    public int TimeSlotId { get; set; }
    public int ProviderServiceId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Notes { get; set; }

    public int? AddressId { get; set; }

    public string? Label { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? StreetAndBuilding { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool SaveNewAddress { get; set; }
}

public sealed class CreateBookingResult
{
    public int BookingId { get; init; }
    public bool NeedsPayment { get; init; }
}

public sealed class AddressDto
{
    public int Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string StreetAndBuilding { get; init; } = string.Empty;
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string Formatted => $"{StreetAndBuilding}, {District}, {City}";
}

