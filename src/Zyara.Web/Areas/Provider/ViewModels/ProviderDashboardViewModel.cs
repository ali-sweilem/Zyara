using System.Collections.Generic;
using Zyara.Domain.Enums;

namespace Zyara.Web.Areas.Provider.ViewModels;

public class ProviderDashboardViewModel
{
    // Provider identity
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsDoctor => Role == UserRole.Doctor;
    public bool IsNurse => Role == UserRole.Nurse;
    public string RoleTitle => IsDoctor ? "Doctor" : "Nurse";
    public string RoleTitleAr => IsDoctor ? "طبيب" : "ممرض / ممرضة";
    public string LicenseNumber { get; set; } = string.Empty;
    public VerificationStatus VerificationStatus { get; set; }
    public string BaseCity { get; set; } = string.Empty;

    // Metrics
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public decimal WalletBalance { get; set; }
    public int TodayVisitsCount { get; set; }
    public int PendingRequestsCount { get; set; }
    public int CompletedVisitsCount { get; set; }
    public decimal TotalEarnings { get; set; }

    // Lists
    public List<BookingItemViewModel> UpcomingVisits { get; set; } = new();
    public List<BookingItemViewModel> PendingRequests { get; set; } = new();
    public List<TimeSlotItemViewModel> TodayTimeSlots { get; set; } = new();
}

