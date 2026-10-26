using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Web.Areas.Provider.ViewModels;

namespace Zyara.Web.Areas.Provider.Controllers;

[Area("Provider")]
public class BookingsController : Controller
{
    public IActionResult Index(string? status, string? role)
    {
        bool isNurse = string.Equals(role, "Nurse", StringComparison.OrdinalIgnoreCase);
        var allVisits = GetSampleBookings(isNurse);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, true, out var parsedStatus))
        {
            allVisits = allVisits.Where(v => v.Status == parsedStatus).ToList();
        }

        ViewBag.CurrentStatusFilter = status ?? "All";
        ViewBag.CurrentRole = isNurse ? "Nurse" : "Doctor";
        return View(allVisits);
    }

    public IActionResult Details(int id, string? role)
    {
        bool isNurse = string.Equals(role, "Nurse", StringComparison.OrdinalIgnoreCase);
        var booking = GetSampleBookings(isNurse).FirstOrDefault(b => b.Id == id) ?? GetSampleBookings(isNurse).First();

        var model = new BookingDetailsViewModel
        {
            Booking = booking,
            CurrentProviderRole = isNurse ? UserRole.Nurse : UserRole.Doctor,
            PatientAge = 64,
            PatientGender = "Male",
            PatientNotes = "Patient has chronic type-2 diabetes and mild hypertension. Complains of sudden dizziness.",
            Prescriptions = new List<PrescriptionItemViewModel>
            {
                new() { MedicationName = "Concor 5mg", Dosage = "1 tablet", Frequency = "Once daily in morning", Duration = "30 days", Instructions = "Take after breakfast with water" },
                new() { MedicationName = "Glucophage 1000mg", Dosage = "1 tablet", Frequency = "Twice daily", Duration = "30 days", Instructions = "Take with principal meals" }
            },
            ClinicalNotes = new ClinicalNotesViewModel
            {
                BloodPressure = "130/85 mmHg",
                HeartRate = "76 bpm",
                Temperature = "36.8 °C",
                BloodSugar = "142 mg/dL (Random)",
                ProceduresDone = isNurse ? "Sterile dressing changed on left foot ulcer; normal saline irrigation performed; vital signs monitored." : "Full cardiovascular and respiratory examination; medication adjustment; patient counseled on low sodium diet.",
                GeneralAssessment = "Patient is oriented and clinically stable. Recovery is progressing as expected."
            }
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult Accept(int id, string? role)
    {
        TempData["SuccessMessage"] = $"Booking #{id} has been accepted and confirmed successfully!";
        return RedirectToAction(nameof(Details), new { id, role });
    }

    [HttpPost]
    public IActionResult Reject(int id, string? role)
    {
        TempData["ErrorMessage"] = $"Booking #{id} has been declined. The patient and support team have been notified.";
        return RedirectToAction(nameof(Index), new { role });
    }

    [HttpPost]
    public IActionResult Start(int id, string otp, string? role)
    {
        if (string.IsNullOrWhiteSpace(otp))
        {
            TempData["ErrorMessage"] = "OTP Code is required to verify arrival and start the home visit.";
            return RedirectToAction(nameof(Details), new { id, role });
        }

        // In a real database call, booking.Start(otp) validates the code
        TempData["SuccessMessage"] = $"OTP code verified! Home visit #{id} is now IN PROGRESS.";
        return RedirectToAction(nameof(Details), new { id, role });
    }

    [HttpPost]
    public IActionResult Complete(int id, string? role)
    {
        TempData["SuccessMessage"] = $"Home visit #{id} marked as COMPLETED! Platform commission was deducted and funds credited to your wallet.";
        return RedirectToAction(nameof(Details), new { id, role });
    }

    private static List<BookingItemViewModel> GetSampleBookings(bool isNurse)
    {
        return new List<BookingItemViewModel>
        {
            new()
            {
                Id = 101,
                PatientName = "Mohamed El-Sayed",
                PatientPhone = "01098765432",
                PatientAddress = "Building 14, 5th Settlement, New Cairo",
                City = "New Cairo",
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
                PatientName = "Fatma Hassan",
                PatientPhone = "01123456789",
                PatientAddress = "42 Abbas El-Akkad St, Nasr City",
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
            },
            new()
            {
                Id = 103,
                PatientName = "Ahmed Khaled",
                PatientPhone = "01234567890",
                PatientAddress = "Villa 22, Rehab City, Group 18",
                City = "Rehab City",
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
            },
            new()
            {
                Id = 104,
                PatientName = "Zeinab Abdelrahman",
                PatientPhone = "01511223344",
                PatientAddress = "18 El-Merghany St, Heliopolis",
                City = "Heliopolis",
                ServiceName = isNurse ? "Elderly Bedside Care & Catheter Flush" : "Routine Chronic Disease Checkup",
                ServiceType = isNurse ? ServiceType.NurseHomeVisit : ServiceType.DoctorHomeVisit,
                Date = DateTime.Today.AddDays(-1),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                TotalPrice = isNurse ? 320.00m : 500.00m,
                ServiceFee = isNurse ? 320.00m : 500.00m,
                Commission = isNurse ? 48.00m : 75.00m,
                Status = BookingStatus.Completed,
                PaymentMethod = PaymentMethod.OnlinePayment,
                PaymentStatus = PaymentStatus.Captured,
                OtpCode = "90124"
            }
        };
    }
}

