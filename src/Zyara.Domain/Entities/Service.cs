using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Entities
{
    public class Service
    {
        public int Id { get; set; }
        public string? Name { get; set; } // Required (Non-nullable)
        public ServiceType Type { get; set; }
        public bool IsActive { get; set; }

        // Service has many provider services & bookings 
        public List<ProviderService>? ProviderServices { get; set; }
        public List<Booking>? Bookings { get; set; }
    }
}
