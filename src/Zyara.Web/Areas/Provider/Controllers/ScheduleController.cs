using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Web.Areas.Provider.ViewModels;

namespace Zyara.Web.Areas.Provider.Controllers;

[Area("Provider")]
public class ScheduleController : Controller
{
    public IActionResult Index(string? role, DateTime? date)
    {
        bool isNurse = string.Equals(role, "Nurse", StringComparison.OrdinalIgnoreCase);
        DateTime targetDate = date ?? DateTime.Today;

        var model = new ScheduleViewModel
        {
            SelectedDate = targetDate,
            Role = isNurse ? UserRole.Nurse : UserRole.Doctor,
            NewSlotDate = targetDate,
            Slots = GetSampleSlots(targetDate)
        };

        ViewBag.CurrentRole = isNurse ? "Nurse" : "Doctor";
        return View(model);
    }

    [HttpPost]
    public IActionResult Create(ScheduleViewModel form, string? role)
    {
        TempData["SuccessMessage"] = $"New availability slot on {form.NewSlotDate:yyyy-MM-dd} from {form.NewSlotStart:hh\\:mm} to {form.NewSlotEnd:hh\\:mm} was added successfully!";
        return RedirectToAction(nameof(Index), new { role, date = form.NewSlotDate.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    public IActionResult Delete(int id, string? role)
    {
        TempData["SuccessMessage"] = $"Time slot #{id} was removed from your schedule.";
        return RedirectToAction(nameof(Index), new { role });
    }

    private static List<TimeSlotItemViewModel> GetSampleSlots(DateTime date)
    {
        return new List<TimeSlotItemViewModel>
        {
            new() { Id = 1, Date = date, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0), Status = TimeSlotStatus.Available },
            new() { Id = 2, Date = date, StartTime = new TimeSpan(10, 30, 0), EndTime = new TimeSpan(11, 30, 0), Status = TimeSlotStatus.Available },
            new() { Id = 3, Date = date, StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(13, 0, 0), Status = TimeSlotStatus.Booked, PatientName = "Mohamed El-Sayed" },
            new() { Id = 4, Date = date, StartTime = new TimeSpan(14, 30, 0), EndTime = new TimeSpan(15, 30, 0), Status = TimeSlotStatus.Booked, PatientName = "Fatma Hassan" },
            new() { Id = 5, Date = date, StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(17, 0, 0), Status = TimeSlotStatus.Available },
            new() { Id = 6, Date = date, StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(19, 0, 0), Status = TimeSlotStatus.Available }
        };
    }
}

