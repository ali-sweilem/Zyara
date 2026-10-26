using System;
using System.Collections.Generic;
using Zyara.Domain.Enums;

namespace Zyara.Web.Areas.Provider.ViewModels;

public class ScheduleViewModel
{
    public DateTime SelectedDate { get; set; } = DateTime.Today;
    public UserRole Role { get; set; }
    public List<TimeSlotItemViewModel> Slots { get; set; } = new();

    // New slot form
    public DateTime NewSlotDate { get; set; } = DateTime.Today;
    public TimeSpan NewSlotStart { get; set; } = new TimeSpan(10, 0, 0);
    public TimeSpan NewSlotEnd { get; set; } = new TimeSpan(11, 0, 0);
}

