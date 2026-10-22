using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Entities
{
    public class ProviderService
    {
        public int Id { get; set; }
        // FK
        public int ProviderId { get; set; }
        public Provider? Provider { get; set; }
        // FK
        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        public decimal Fee { get; set; }
        public decimal CommissionPercentage { get; set; }
    }
}
