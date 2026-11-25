using System;

namespace Zyara.Domain.Entities
{
    public class PlatformSettings
    {
        public int Id { get; set; } = 1;
        /// <summary>The platform's percentage of every visit (taken from the provider's earnings after the visit).</summary>
        public decimal ServiceFeePercentage { get; set; } = 15.0m;

        /// <summary>
        /// The percentage of the visit price charged for a late cancellation: patient cancels a confirmed booking
        /// less than 1 hour before the visit, or the provider cancels on the day of the visit.
        /// </summary>
        public decimal CommissionPercentage { get; set; } = 15.0m;
    }
}


