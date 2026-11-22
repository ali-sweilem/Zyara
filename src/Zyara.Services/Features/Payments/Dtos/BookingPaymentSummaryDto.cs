using System;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Payments.Dtos;

public class BookingPaymentSummaryDto
{
    public int BookingId { get; set; }
    public int PatientId { get; set; }
    public string ProviderFullName { get; set; } = string.Empty;
    public string? ProviderSpecialty { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal ServiceFee { get; set; }
    public string? VisitAddress { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public bool IsFakeGatewayActive { get; set; }
    public string FormattedWindow { get; set; } = string.Empty;
    public string DayLabel { get; set; } = string.Empty;
}

