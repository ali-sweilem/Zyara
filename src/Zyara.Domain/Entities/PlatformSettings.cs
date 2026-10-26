using System;

namespace Zyara.Domain.Entities
{
    public class PlatformSettings
    {
        public int Id { get; set; } = 1;
        public decimal ServiceFee { get; set; }
        public decimal CommissionPercentage { get; set; } = 15.0m;
    }
}

