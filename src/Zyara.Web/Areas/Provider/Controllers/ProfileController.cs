using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Web.Areas.Provider.ViewModels;

namespace Zyara.Web.Areas.Provider.Controllers;

[Area("Provider")]
public class ProfileController : Controller
{
    public IActionResult Index(string? role)
    {
        bool isNurse = string.Equals(role, "Nurse", StringComparison.OrdinalIgnoreCase);

        var model = new ProviderProfileViewModel
        {
            Id = isNurse ? 2 : 1,
            FullName = isNurse ? "Salma Ibrahim" : "Dr. Hazem El-Menshawy",
            Email = isNurse ? "salma.ibrahim@zyara-health.com" : "dr.hazem@zyara-health.com",
            Phone = isNurse ? "01099887766" : "01288776655",
            Role = isNurse ? UserRole.Nurse : UserRole.Doctor,
            Specialty = isNurse ? "Critical Care & Home Nursing" : "Internal Medicine & Geriatrics",
            ExperienceYears = isNurse ? 7 : 14,
            LicenseNumber = isNurse ? "RN-EGY-84920" : "MD-EGY-10492",
            BaseCity = "Cairo (New Cairo & Nasr City)",
            VerificationStatus = VerificationStatus.Verified,
            VerifiedAt = DateTime.UtcNow.AddMonths(-4),
            WalletBalance = 2850.00m,
            AverageRating = 4.9,
            RatingCount = 84,
            Credentials = new List<CredentialItemViewModel>
            {
                new()
                {
                    DocumentType = isNurse ? "Nursing Syndicate Registration Card (كارنيه نقابة التمريض)" : "Medical Syndicate Registration Card (كارنيه نقابة الأطباء)",
                    FileName = isNurse ? "syndicate_card_nurse.pdf" : "syndicate_card_doctor.pdf",
                    UploadedAt = DateTime.UtcNow.AddMonths(-4),
                    IsVerified = true
                },
                new()
                {
                    DocumentType = "Ministry of Health License to Practice (ترخيص مزاولة المهنة)",
                    FileName = "moh_practice_license.pdf",
                    UploadedAt = DateTime.UtcNow.AddMonths(-4),
                    IsVerified = true
                },
                new()
                {
                    DocumentType = isNurse ? "B.Sc. in Nursing Science (بكالوريوس علوم التمريض)" : "Master's Degree in Internal Medicine (ماجستير أمراض الباطنة)",
                    FileName = isNurse ? "degree_nursing.pdf" : "degree_internal_med.pdf",
                    UploadedAt = DateTime.UtcNow.AddMonths(-4),
                    IsVerified = true
                }
            }
        };

        ViewBag.CurrentRole = isNurse ? "Nurse" : "Doctor";
        return View(model);
    }

    [HttpPost]
    public IActionResult RequestPayout(string? role)
    {
        TempData["SuccessMessage"] = "Withdrawal request of 2,850.00 EGP submitted! Funds will be transferred to your registered InstaPay / Bank Account within 24 hours.";
        return RedirectToAction(nameof(Index), new { role });
    }
}

