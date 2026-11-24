using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Providers.Dtos
{
    public class TimeSlotDto
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

    public class CreateTimeSlotDto
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

    /// <summary>A fixed weekly working time: "every Sunday 10:00 - 12:00".</summary>
    public class WeeklySlotDto
    {
        public int Id { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class CreateWeeklySlotDto
    {
        [Required(ErrorMessage = "Choose the day.")]
        [Display(Name = "Day")]
        public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Saturday;

        [Required(ErrorMessage = "Start time is required.")]
        [Display(Name = "Start Time")]
        public TimeSpan? StartTime { get; set; }
    }

    public class ProviderTimeSlotIndexDto
    {
        public List<TimeSlotDto> TimeSlots { get; set; } = new();
        public CreateTimeSlotDto NewTimeSlot { get; set; } = new();
        public List<WeeklySlotDto> WeeklySlots { get; set; } = new();
    }
}

