using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Web.Areas.Provider.ViewModels
{
    public class TimeSlotVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        [Display(Name = "From")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        [Display(Name = "To")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Status")]
        public TimeSlotStatus Status { get; set; } = TimeSlotStatus.Available;

        public int ProviderId { get; set; }
    }

    public class CreateTimeSlotVM
    {
        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Appointment start time is required")]
        public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

        [Required(ErrorMessage = "Appointment end time is required")]
        public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0);

        [Range(15, 120, ErrorMessage = "Session duration must be between 15 and 120 minutes")]
        public int SlotDurationMinutes { get; set; } = 30;
    }

    public class ProviderTimeSlotIndexVM
    {
        public List<TimeSlotVM> TimeSlots { get; set; } = new List<TimeSlotVM>();
        public CreateTimeSlotVM NewTimeSlot { get; set; } = new CreateTimeSlotVM();
    }
}