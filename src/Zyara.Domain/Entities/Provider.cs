using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Entities
{
    public abstract class Provider :User
    {
        public string Specilty { get; set; }
        public int experienceYears { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public decimal averageRating { get; set; }
        public List<Booking> Booking { get; set; }
        public List<TimeSlot> TimeSlots { get; set; }
        public List<ProviderCredential> ProviderCredentials { get; set; }
        public int AdminID { get; set; }
        public Admin Admin { get; set; }
    }
}
