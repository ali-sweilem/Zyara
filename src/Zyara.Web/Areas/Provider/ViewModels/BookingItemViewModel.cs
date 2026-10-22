using System;
using Zyara.Domain.Enums;

namespace Zyara.Web.Areas.Provider.ViewModels;

public class BookingItemViewModel
{
    public int Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string PatientAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal Commission { get; set; }
    public decimal NetEarnings => TotalPrice - Commission;
    public BookingStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string OtpCode { get; set; } = string.Empty;
    public bool RequiresOtp => ServiceType == ServiceType.DoctorHomeVisit || ServiceType == ServiceType.NurseHomeVisit;
}

public class TimeSlotItemViewModel
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSlotStatus Status { get; set; }
    public string? PatientName { get; set; }
}

