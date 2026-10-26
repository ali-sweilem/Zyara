using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Web.Areas.Provider.ViewModels;

namespace Zyara.Web.Areas.Provider.Controllers;

[Area("Provider")]
public class ServicesController : Controller
{
    public IActionResult Index(string? role)
    {
        bool isNurse = string.Equals(role, "Nurse", StringComparison.OrdinalIgnoreCase);

        var model = new ProviderServicesViewModel
        {
            Role = isNurse ? UserRole.Nurse : UserRole.Doctor,
            Services = isNurse ? GetNurseServices() : GetDoctorServices()
        };

        ViewBag.CurrentRole = isNurse ? "Nurse" : "Doctor";
        return View(model);
    }

    [HttpPost]
    public IActionResult UpdateFee(int id, decimal fee, string? role)
    {
        TempData["SuccessMessage"] = $"Service fee updated to {fee:N0} EGP successfully!";
        return RedirectToAction(nameof(Index), new { role });
    }

    private static List<ProviderServiceItemViewModel> GetDoctorServices()
    {
        return new List<ProviderServiceItemViewModel>
        {
            new()
            {
                Id = 1,
                ServiceId = 1,
                Name = "Comprehensive Home Medical Consultation (كشف منزلي شامل)",
                Description = "In-depth physical examination, vital review, diagnosis, and official digital prescription.",
                ServiceType = ServiceType.DoctorHomeVisit,
                BaseFee = 600.00m,
                CommissionPercentage = 15.0m,
                IsActive = true
            },
            new()
            {
                Id = 2,
                ServiceId = 2,
                Name = "Elderly & Chronic Disease Home Follow-up (متابعة أمراض مزمنة)",
                Description = "Dedicated care for hypertension, diabetes, and cardiovascular regular monitoring at home.",
                ServiceType = ServiceType.DoctorHomeVisit,
                BaseFee = 500.00m,
                CommissionPercentage = 15.0m,
                IsActive = true
            },
            new()
            {
                Id = 3,
                ServiceId = 3,
                Name = "Urgent Urgent Home Medical Visit (زيارة طارئة منزلية)",
                Description = "Immediate clinical assessment for acute non-life threatening episodes.",
                ServiceType = ServiceType.DoctorHomeVisit,
                BaseFee = 750.00m,
                CommissionPercentage = 15.0m,
                IsActive = true
            }
        };
    }

    private static List<ProviderServiceItemViewModel> GetNurseServices()
    {
        return new List<ProviderServiceItemViewModel>
        {
            new()
            {
                Id = 11,
                ServiceId = 11,
                Name = "Post-Surgical Sterile Wound Care & Dressing (غيار جروح معقم)",
                Description = "Aseptic technique wound cleaning, suture check, sterile dressing, and healing documentation.",
                ServiceType = ServiceType.NurseHomeVisit,
                BaseFee = 350.00m,
                CommissionPercentage = 15.0m,
                IsActive = true
            },
            new()
            {
                Id = 12,
                ServiceId = 12,
                Name = "IV Cannulation & Medication Infusion (تركيب كانيولا ومحاليل)",
                Description = "Peripheral vein cannulation, IV fluid infusion, and prescribed antibiotic administration.",
                ServiceType = ServiceType.NurseHomeVisit,
                BaseFee = 280.00m,
                CommissionPercentage = 15.0m,
                IsActive = true
            },
            new()
            {
                Id = 13,
                ServiceId = 13,
                Name = "Urinary Catheter Care & Replacement (رعاية وتركيب قسطرة)",
                Description = "Professional catheter placement, bladder irrigation, and hygiene management.",
                ServiceType = ServiceType.NurseHomeVisit,
                BaseFee = 380.00m,
                CommissionPercentage = 15.0m,
                IsActive = true
            },
            new()
            {
                Id = 14,
                ServiceId = 14,
                Name = "Vitals, ECG & Blood Glucose Monitoring (فحص علامات حيوية وسكر)",
                Description = "Blood pressure, oxygen saturation, temperature, and blood sugar testing with clinical log.",
                ServiceType = ServiceType.NurseHomeVisit,
                BaseFee = 200.00m,
                CommissionPercentage = 15.0m,
                IsActive = true
            }
        };
    }
}

