using System;
using System.Collections.Generic;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Providers.Dtos;

public class ProviderDashboardDto
{
    public int ProviderId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Specialty { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public VerificationStatus VerificationStatus { get; set; }
    public string? RejectionReason { get; set; }

    public string TodayDateEgyptFormatted { get; set; } = string.Empty;

    // KPIs
    public int PendingRequestsCount { get; set; }
    public int TodayVisitsCount { get; set; }
    public int CompletedThisMonthCount { get; set; }
    public decimal NetEarningsThisMonth { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public decimal WalletBalance { get; set; }

    // Cards
    public IReadOnlyList<ProviderRequestCardDto> RequestsWaiting { get; set; } = [];
    public IReadOnlyList<ProviderVisitItemDto> TodayAndUpcomingVisits { get; set; } = [];
    public int AvailableWindowsNext7Days { get; set; }
    public IReadOnlyList<ProviderReviewDto> LatestReviews { get; set; } = [];
}

public class ProviderRequestCardDto
{
    public int BookingId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientNotes { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string FormattedWindow { get; set; } = string.Empty;
    public string DayLabel { get; set; } = string.Empty;

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }

    public decimal ServiceFee { get; set; }
    public decimal Commission { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal NetEarnings => TotalPrice - ServiceFee - Commission;

    public int TimeLeftMinutes { get; set; }
    public string TimeLeftDisplay { get; set; } = string.Empty;
}

public class ProviderVisitItemDto
{
    public int BookingId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? PatientAddress { get; set; }
    public double? PatientLatitude { get; set; }
    public double? PatientLongitude { get; set; }

    public string ServiceName { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string FormattedWindow { get; set; } = string.Empty;
    public string DayLabel { get; set; } = string.Empty;

    public BookingStatus Status { get; set; }
    public string? StatusReason { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }

    public decimal TotalPrice { get; set; }
    public decimal NetEarnings { get; set; }
    public bool IsHomeVisit { get; set; }
    public string? OtpCode { get; set; }
    public int OtpFailedAttempts { get; set; }
    public bool OtpBlocked => OtpFailedAttempts >= 5;
}

public class ProviderVisitDetailsDto
{
    public int BookingId { get; set; }
    public BookingStatus Status { get; set; }
    public string? StatusReason { get; set; }
    public bool IsActive { get; set; }

    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? PatientAddress { get; set; }
    public double? PatientLatitude { get; set; }
    public double? PatientLongitude { get; set; }
    public bool CanSeePatientContact { get; set; }

    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public string? ServiceDescription { get; set; }

    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string FormattedWindow { get; set; } = string.Empty;
    public string DayLabel { get; set; } = string.Empty;

    public string? PatientNotes { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal Commission { get; set; }
    public decimal NetEarnings => TotalPrice - ServiceFee - Commission;

    public DateTime CreatedAtEgypt { get; set; }
    public DateTime? RespondedAtEgypt { get; set; }
    public DateTime? StartedAtEgypt { get; set; }
    public DateTime? CompletedAtEgypt { get; set; }

    public bool IsHomeVisit { get; set; }
    public string? OtpCode { get; set; }
    public int OtpFailedAttempts { get; set; }
    public bool OtpBlocked => OtpFailedAttempts >= 5;
}

