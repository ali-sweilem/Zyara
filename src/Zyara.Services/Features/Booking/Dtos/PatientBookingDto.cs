using System;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Bookings.Dtos;

public sealed class PatientBookingDto
{
    public int Id { get; init; }
    public BookingStatus Status { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public DateTime BookingDate { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public decimal ServiceFee { get; init; }
    public decimal TotalPrice { get; init; }
}