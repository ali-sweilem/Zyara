using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Entities
{
    public class Prescription
    {
        public int Id { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

      
        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;

       
        public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
    }
}