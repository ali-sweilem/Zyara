

using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; 
using System.Threading.Tasks;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
using Zyara.Web.Areas.Provider.ViewModels;

namespace Zyara.Web.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ScheduleController : Controller
    {
        private readonly ZyaraDbContext _context;

        public ScheduleController(ZyaraDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// دالة مركزية لجلب الـ ID الخاص بالطبيب الحالي بناءً على الـ Authentication (Identity)
        /// </summary>
        private int GetCurrentProviderId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // لو تم تسجيل الدخول بنجاح، نحول الـ ID إلى رقم صحيح، وإلا نرجع 0 أو القيمة الافتراضية
            if (int.TryParse(userIdString, out int providerId))
            {
                return providerId;
            }

            // قيمة افتراضية في حال لم يتم تسجيل الدخول بعد (لتجنب الـ NullReference)
            return 1;
        }

        [HttpGet]
        public IActionResult Index()
        {
            int currentProviderId = GetCurrentProviderId();

            var model = new ProviderTimeSlotIndexVM
            {
                TimeSlots = _context.TimeSlots
                    .Where(t => t.ProviderId == currentProviderId)
                    .OrderBy(t => t.Date)
                    .ThenBy(t => t.StartTime)
                    .Select(t => new TimeSlotVM
                    {
                        Id = t.Id,
                        Date = t.Date,
                        StartTime = t.StartTime,
                        EndTime = t.EndTime,
                        Status = t.Status,
                        ProviderId = t.ProviderId
                    }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTimeSlot(ProviderTimeSlotIndexVM model)
        {
            var newSlot = model.NewTimeSlot;
            int currentProviderId = GetCurrentProviderId();

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
                model.TimeSlots = _context.TimeSlots
                    .Where(t => t.ProviderId == currentProviderId)
                    .Select(t => new TimeSlotVM
                    {
                        Id = t.Id,
                        Date = t.Date,
                        StartTime = t.StartTime,
                        EndTime = t.EndTime,
                        Status = t.Status,
                        ProviderId = t.ProviderId
                    }).ToList();

                return View("Index", model);
            }

            var timeSlotEntity = new TimeSlot
            {
                Date = newSlot.Date,
                StartTime = newSlot.StartTime,
                EndTime = newSlot.EndTime,
                SlotDurationMinutes = newSlot.SlotDurationMinutes,
                Status = TimeSlotStatus.Available,
                ProviderId = currentProviderId
            };

            _context.TimeSlots.Add(timeSlotEntity);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "تم إضافة الـ TimeSlot بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTimeSlot(int id)
        {
            var slot = _context.TimeSlots.Find(id);
            if (slot != null)
            {
                if (slot.Status == TimeSlotStatus.Available)
                {
                    _context.TimeSlots.Remove(slot);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "تم حذف الـ TimeSlot بنجاح.";
                }
                else
                {
                    TempData["ErrorMessage"] = "لا يمكن حذف موعد محجوز أو مغلق.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}