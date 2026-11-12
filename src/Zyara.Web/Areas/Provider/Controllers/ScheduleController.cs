using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        private int GetCurrentProviderId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int providerId))
            {
                return providerId;
            }

            // CurrentUser.ProviderId = 1 as per team agreement
            return 1;
        }

        [HttpGet]
        public IActionResult Index()
        {
            int currentProviderId = GetCurrentProviderId();

            var model = new ProviderTimeSlotIndexVM
            {
                TimeSlots = GetProviderSlots(currentProviderId)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTimeSlot(ProviderTimeSlotIndexVM model)
        {
            int currentProviderId = GetCurrentProviderId();
            var newSlot = model.NewTimeSlot;

            // 1. حساب وقت النهاية تلقائياً بناءً على قاعدة الـ 120 دقيقة
            var duration = TimeSpan.FromMinutes(120);
            var calculatedEndTime = newSlot.StartTime.Add(duration);

            // 2. التحقق من التاريخ
            if (newSlot.Date.Date < DateTime.Today)
            {
                ModelState.AddModelError("NewTimeSlot.Date", "Cannot schedule slots in the past.");
            }

            // 3. التحقق من انتهاء اليوم (Midnight overflow)
            if (calculatedEndTime > TimeSpan.FromHours(24) || calculatedEndTime <= newSlot.StartTime)
            {
                ModelState.AddModelError("NewTimeSlot.StartTime", "Slot exceeds the 24-hour day boundary.");
            }

            // 4. التحقق من التضارب مع مواعيد أخرى لنفس مقدم الخدمة في نفس اليوم
            bool hasOverlap = _context.TimeSlots
                .Any(t => t.ProviderId == currentProviderId
                          && t.Date.Date == newSlot.Date.Date
                          && t.StartTime < calculatedEndTime
                          && t.EndTime > newSlot.StartTime);

            if (hasOverlap)
            {
                ModelState.AddModelError("NewTimeSlot.StartTime", "This slot overlaps with an existing time slot on this date.");
            }

            if (!ModelState.IsValid)
            {
                model.TimeSlots = GetProviderSlots(currentProviderId);
                return View("Index", model);
            }

            // 5. حفظ الموعد الجديد
            var timeSlotEntity = new TimeSlot
            {
                Date = newSlot.Date.Date,
                StartTime = newSlot.StartTime,
                EndTime = calculatedEndTime,
                SlotDurationMinutes = 120,
                Status = TimeSlotStatus.Available,
                ProviderId = currentProviderId
            };

            _context.TimeSlots.Add(timeSlotEntity);
            _context.SaveChanges();

            TempData["Message"] = "New 2-hour time slot added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTimeSlot(int id)
        {
            int currentProviderId = GetCurrentProviderId();

            var slot = _context.TimeSlots
                .FirstOrDefault(t => t.Id == id && t.ProviderId == currentProviderId);

            if (slot == null)
            {
                TempData["Error"] = "Time slot not found.";
                return RedirectToAction(nameof(Index));
            }

            // الحذف مسموح فقط إذا كان الموعد متاحاً
            if (slot.Status == TimeSlotStatus.Available)
            {
                _context.TimeSlots.Remove(slot);
                _context.SaveChanges();
                TempData["Message"] = "Time slot deleted successfully.";
            }
            else
            {
                TempData["Error"] = $"Cannot delete a slot that is currently {slot.Status}.";
            }

            return RedirectToAction(nameof(Index));
        }

        private List<TimeSlotVM> GetProviderSlots(int providerId)
        {
            return _context.TimeSlots
                .AsNoTracking()
                .Where(t => t.ProviderId == providerId)
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
                })
                .ToList();
        }
    }
}