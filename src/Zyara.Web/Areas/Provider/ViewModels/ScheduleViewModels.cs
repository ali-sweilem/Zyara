using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Web.Areas.Provider.ViewModels
{
    public class TimeSlotVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "تاريخ الموعد مطلوب")]
        [Display(Name = "التاريخ")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "وقت البداية مطلوب")]
        [Display(Name = "من الساعة")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "وقت النهاية مطلوب")]
        [Display(Name = "إلى الساعة")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "الحالة")]
        public TimeSlotStatus Status { get; set; } = TimeSlotStatus.Available;

        public int ProviderId { get; set; }
    }

    public class CreateTimeSlotVM
    {
        [Required(ErrorMessage = "تاريخ اليوم مطلوب")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "وقت بداية الموعد مطلوب")]
        public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

        [Required(ErrorMessage = "وقت نهاية الموعد مطلوب")]
        public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0);

        [Range(15, 120, ErrorMessage = "مدة الجلسة تكون بين 15 و 120 دقيقة")]
        public int SlotDurationMinutes { get; set; } = 30;
    }

    public class ProviderTimeSlotIndexVM
    {
        public List<TimeSlotVM> TimeSlots { get; set; } = new List<TimeSlotVM>();
        public CreateTimeSlotVM NewTimeSlot { get; set; } = new CreateTimeSlotVM();
    }
}