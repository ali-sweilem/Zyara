using System;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Entities
{
    public class TimeSlot
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSlotStatus Status { get; set; } = TimeSlotStatus.Available;
        public int ProviderId { get; set; }
        public Provider? Provider { get; set; }
    }
}

