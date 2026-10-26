using System;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;

namespace Zyara.Domain.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public BookingStatus Status { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Commission { get; set; }
        public string OtpCode { get; set; } = string.Empty;

        // FKs
        public int PatientId { get; set; }
        public int ProviderId { get; set; }
        public int ServiceId { get; set; }
        public int TimeSlotId { get; set; }

        // Navigation properties
        public Patient? Patient { get; set; }
        // TODO(M2): change to Provider once the class exists
        public Provider? Provider { get; set; }
        public Service? Service { get; set; }
        public TimeSlot? TimeSlot { get; set; }
        public Payment? Payment { get; set; }

        public Rating? Rating { get; set; }

    }
}
