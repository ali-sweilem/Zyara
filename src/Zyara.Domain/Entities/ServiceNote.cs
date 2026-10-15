using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Entities
{
    public class ServiceNote
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;

        
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
    }
}