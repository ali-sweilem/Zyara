using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Web.Areas.Provider.ViewModels
{
    public class TimeSlotVM
    {
        public int Id { get; set; }

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "From")]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "To")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Status")]
        public TimeSlotStatus Status { get; set; } = TimeSlotStatus.Available;

        public int ProviderId { get; set; }
    }

    public class CreateTimeSlotVM
    {
        [Required(ErrorMessage = "Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Start time is required.")]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; } = new TimeSpan(10, 0, 0); // 10:00 AM default

        [Display(Name = "Slot Duration (Minutes)")]
        public int SlotDurationMinutes { get; set; } = 120; // 120 minutes fixed by business rule
    }

    public class ProviderTimeSlotIndexVM
    {
        public List<TimeSlotVM> TimeSlots { get; set; } = new();
        public CreateTimeSlotVM NewTimeSlot { get; set; } = new();
    }
}