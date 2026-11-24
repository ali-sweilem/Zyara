using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Web.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ScheduleController : Controller
    {
        private readonly ZyaraDbContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly IAuthorizationService _auth;
        private readonly IWeeklyScheduleService _weekly;

        public ScheduleController(
            ZyaraDbContext context,
            ICurrentUser currentUser,
            IAuthorizationService auth,
            IWeeklyScheduleService weekly)
        {
            _context = context;
            _currentUser = currentUser;
            _auth = auth;
            _weekly = weekly;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            int currentProviderId = _currentUser.RequireUserId();

            var model = new ProviderTimeSlotIndexDto
            {
                TimeSlots = GetProviderSlots(currentProviderId),
                WeeklySlots = await _weekly.GetAsync(currentProviderId, cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTimeSlot(ProviderTimeSlotIndexDto model)
        {
            int currentProviderId = _currentUser.RequireUserId();
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
                model.WeeklySlots = await _weekly.GetAsync(currentProviderId);
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
        public async Task<IActionResult> DeleteTimeSlot(int id)
        {
            var slot = await _context.TimeSlots
                .FirstOrDefaultAsync(t => t.Id == id);

            if (slot == null)
            {
                return NotFound();
            }

            var authorization = await _auth.AuthorizeAsync(User, slot, Operations.Manage);
            if (!authorization.Succeeded)
            {
                return NotFound();
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

        // POST /Provider/Schedule/AddWeekly : a fixed weekly time (repeats every week)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWeekly(
            [Bind(Prefix = "NewWeeklySlot")] CreateWeeklySlotDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                                    ?? "Choose a day and a start time.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _weekly.AddAsync(_currentUser.RequireUserId(), dto, cancellationToken);

            if (result.Succeeded) TempData["Message"] = "Weekly time added. Slots were created for the next 4 weeks and new weeks are added automatically.";
            else TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Index));
        }

        // POST /Provider/Schedule/RemoveWeekly/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveWeekly(int id, CancellationToken cancellationToken)
        {
            var result = await _weekly.RemoveAsync(_currentUser.RequireUserId(), id, cancellationToken);

            if (result.Succeeded) TempData["Message"] = "Weekly time removed. Its free upcoming slots were deleted; booked ones stay.";
            else TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Index));
        }

        private List<TimeSlotDto> GetProviderSlots(int providerId)
        {
            return _context.TimeSlots
                .AsNoTracking()
                .Where(t => t.ProviderId == providerId)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.StartTime)
                .Select(t => new TimeSlotDto
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

