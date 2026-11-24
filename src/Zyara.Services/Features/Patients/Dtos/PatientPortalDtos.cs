using System;
using System.Collections.Generic;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Patients.Dtos;

public sealed class PatientDashboardDto
{
    public string PatientName { get; init; } = string.Empty;
    public IReadOnlyList<PatientBookingDto> Bookings { get; init; } = Array.Empty<PatientBookingDto>();
    public IReadOnlyList<PatientAddressDto> Addresses { get; init; } = Array.Empty<PatientAddressDto>();
}

public sealed class PatientBookingDto
{
    public int Id { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public BookingStatus Status { get; init; }
    public DateOnly? Date { get; init; }
    public TimeSpan? StartTime { get; init; }
    public bool CanCancel => Status is BookingStatus.PendingProviderApproval or BookingStatus.Confirmed;
    public bool HasReview { get; init; }
    public bool CanReview => Status == BookingStatus.Completed && !HasReview;
}

public sealed class PatientAddressDto
{
    public int Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string FullAddress { get; init; } = string.Empty;
}
