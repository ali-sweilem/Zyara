using System;

namespace Zyara.Domain.Entities
{
    /// <summary>
    /// A fixed weekly working time of a provider ("every Sunday at 10:00").
    /// The real bookable TimeSlot rows are generated from it for the coming weeks.
    /// </summary>
    public class WeeklySlotTemplate
    {
        public int Id { get; set; }

        public int ProviderId { get; set; }
        public Provider? Provider { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public int SlotDurationMinutes { get; set; } = 120;

        /// <summary>
        /// Slots were already created up to this date. Generation only continues after it,
        /// so a slot the provider deleted on purpose (a holiday) is never recreated.
        /// </summary>
        public DateTime GeneratedThrough { get; set; }
    }
}
