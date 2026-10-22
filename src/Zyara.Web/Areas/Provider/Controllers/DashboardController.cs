using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Web.Areas.Provider.ViewModels;

namespace Zyara.Web.Areas.Provider.Controllers;

[Area("Provider")]
public class DashboardController : Controller
{
    public IActionResult Index(string? role)
    {
        bool isNurse = string.Equals(role, "Nurse", StringComparison.OrdinalIgnoreCase);

        var model = new ProviderDashboardViewModel
        {
            Id = isNurse ? 2 : 1,
            FullName = isNurse ? "Salma Ibrahim" : "Dr. Hazem El-Menshawy",
            Specialty = isNurse ? "Critical Care & Home Nursing" : "Internal Medicine & Geriatrics",
            Role = isNurse ? UserRole.Nurse : UserRole.Doctor,
            LicenseNumber = isNurse ? "RN-EGY-84920" : "MD-EGY-10492",
            VerificationStatus = VerificationStatus.Verified,
            BaseCity = "Cairo (New Cairo & Nasr City)",
            AverageRating = 4.9,
            RatingCount = 84,
            WalletBalance = 2850.00m,
            TodayVisitsCount = 3,
            PendingRequestsCount = 2,
            CompletedVisitsCount = 142,
            TotalEarnings = 36800.00m,
            UpcomingVisits = GetSampleUpcomingVisits(isNurse),
            PendingRequests = GetSamplePendingRequests(isNurse),
            TodayTimeSlots = GetSampleTodaySlots()
        };

        return View(model);
    }

    private static List<BookingItemViewModel> GetSampleUpcomingVisits(bool isNurse)
    {
        return new List<BookingItemViewModel>
        {
            new()
            {
                Id = 101,
                PatientName = "Mohamed El-Sayed (68 yrs)",
                PatientPhone = "01098765432",
                PatientAddress = "Building 14, 5th Settlement, New Cairo",
                City = "Cairo",
                ServiceName = isNurse ? "Post-Operative Wound Care & Vitals" : "Comprehensive Elderly Medical Assessment",
                ServiceType = isNurse ? ServiceType.NurseHomeVisit : ServiceType.DoctorHomeVisit,
                Date = DateTime.Today,
                StartTime = new TimeSpan(11, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                TotalPrice = isNurse ? 350.00m : 600.00m,
                ServiceFee = isNurse ? 350.00m : 600.00m,
                Commission = isNurse ? 52.50m : 90.00m,
                Status = BookingStatus.Confirmed,
                PaymentMethod = PaymentMethod.OnlinePayment,
                PaymentStatus = PaymentStatus.Captured,
                OtpCode = "48291"
            },
            new()
            {
                Id = 102,
                PatientName = "Fatma Hassan (45 yrs)",
                PatientPhone = "01123456789",
                PatientAddress = "Abbas El-Akkad St, Nasr City",
                City = "Cairo",
                ServiceName = isNurse ? "IV Cannulation & Medication Infusion" : "Hypertension & Diabetes Follow-up",
                ServiceType = isNurse ? ServiceType.NurseHomeVisit : ServiceType.DoctorHomeVisit,
                Date = DateTime.Today,
                StartTime = new TimeSpan(14, 30, 0),
                EndTime = new TimeSpan(15, 30, 0),
                TotalPrice = isNurse ? 280.00m : 550.00m,
                ServiceFee = isNurse ? 280.00m : 550.00m,
                Commission = isNurse ? 42.00m : 82.50m,
                Status = BookingStatus.Started,
                PaymentMethod = PaymentMethod.CashOnArrival,
                PaymentStatus = PaymentStatus.Pending,
                OtpCode = "73910"
            }
        };
    }

    private static List<BookingItemViewModel> GetSamplePendingRequests(bool isNurse)
    {
        return new List<BookingItemViewModel>
        {
            new()
            {
                Id = 103,
                PatientName = "Ahmed Khaled (52 yrs)",
                PatientPhone = "01234567890",
                PatientAddress = "Villa 22, Rehab City, Group 18",
                City = "Cairo",
                ServiceName = isNurse ? "Emergency Vitals & Nebulizer Session" : "Urgent Home Medical Consultation",
                ServiceType = isNurse ? ServiceType.NurseHomeVisit : ServiceType.DoctorHomeVisit,
                Date = DateTime.Today.AddDays(1),
                StartTime = new TimeSpan(16, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                TotalPrice = isNurse ? 400.00m : 700.00m,
                ServiceFee = isNurse ? 400.00m : 700.00m,
                Commission = isNurse ? 60.00m : 105.00m,
                Status = BookingStatus.PendingProviderApproval,
                PaymentMethod = PaymentMethod.OnlinePayment,
                PaymentStatus = PaymentStatus.Authorized,
                OtpCode = "19482"
            }
        };
    }

    private static List<TimeSlotItemViewModel> GetSampleTodaySlots()
    {
        return new List<TimeSlotItemViewModel>
        {
            new() { Id = 1, Date = DateTime.Today, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 0, 0), Status = TimeSlotStatus.Available },
            new() { Id = 2, Date = DateTime.Today, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 0, 0), Status = TimeSlotStatus.Booked, PatientName = "Mohamed El-Sayed" },
            new() { Id = 3, Date = DateTime.Today, StartTime = new TimeSpan(14, 30, 0), EndTime = new TimeSpan(15, 30, 0), Status = TimeSlotStatus.Booked, PatientName = "Fatma Hassan" },
            new() { Id = 4, Date = DateTime.Today, StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(19, 0, 0), Status = TimeSlotStatus.Available }
        };
    }
}

