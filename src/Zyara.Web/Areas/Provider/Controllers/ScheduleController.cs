

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
        /// Gets the current provider ID from the authenticated identity.
        /// </summary>
        private int GetCurrentProviderId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Return the authenticated provider ID when it can be parsed.
            if (int.TryParse(userIdString, out int providerId))
            {
                return providerId;
            }

            // Use a default value when no provider identity is available.
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
                ModelState.AddModelError("NewTimeSlot.Date", "You cannot add appointments for a past date.");
            }

            if (newSlot.EndTime <= newSlot.StartTime)
            {
                ModelState.AddModelError("NewTimeSlot.EndTime", "End time must be after start time.");
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

            TempData["SuccessMessage"] = "The time slot was added successfully!";
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
                    TempData["SuccessMessage"] = "The time slot was deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "A booked or closed appointment cannot be deleted.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}