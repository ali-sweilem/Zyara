using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Web.Areas.Provider.ViewModels;

namespace Zyara.Web.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ScheduleController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var model = new ProviderTimeSlotIndexVM
            {
                TimeSlots = GetProviderTimeSlots()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTimeSlot(ProviderTimeSlotIndexVM model)
        {
            var newSlot = model.NewTimeSlot;

            if (newSlot.Date < DateTime.Today)
            {
                ModelState.AddModelError("NewTimeSlot.Date", "لا يمكنك إضافة مواعيد في تاريخ قديم.");
            }

            if (newSlot.EndTime <= newSlot.StartTime)
            {
                ModelState.AddModelError("NewTimeSlot.EndTime", "وقت النهاية يجب أن يكون بعد وقت البداية.");
            }

            if (!ModelState.IsValid)
            {
                model.TimeSlots = GetProviderTimeSlots();
                return View("Index", model);
            }

            TempData["SuccessMessage"] = "تم إضافة الـ TimeSlot المتاح بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTimeSlot(int id)
        {
            TempData["SuccessMessage"] = "تم حذف الـ TimeSlot بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        private List<TimeSlotVM> GetProviderTimeSlots()
        {
            return new List<TimeSlotVM>
            {
                new TimeSlotVM { Id = 1, Date = DateTime.Today, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(10, 30, 0), Status = TimeSlotStatus.Available, ProviderId = 1 },
                new TimeSlotVM { Id = 2, Date = DateTime.Today, StartTime = new TimeSpan(10, 30, 0), EndTime = new TimeSpan(11, 0, 0), Status = TimeSlotStatus.Booked, ProviderId = 1 },
                new TimeSlotVM { Id = 3, Date = DateTime.Today, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(11, 30, 0), Status = TimeSlotStatus.Locked, ProviderId = 1 },
                new TimeSlotVM { Id = 4, Date = DateTime.Today.AddDays(1), StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(12, 30, 0), Status = TimeSlotStatus.Available, ProviderId = 1 }
            };
        }
    }
}