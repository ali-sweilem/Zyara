using System;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Payments.Dtos;

public class BookingConfirmationDto
{
    public int BookingId { get; set; }
    public BookingStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public string? OtpCode { get; set; }
    public bool IsHomeVisit { get; set; }
    public int PatientId { get; set; }

    public string ProviderFullName { get; set; } = string.Empty;
    public string? ProviderSpecialty { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? VisitAddress { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string FormattedWindow { get; set; } = string.Empty;
    public string DayLabel { get; set; } = string.Empty;
}
