using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Entities
{
    /// <summary>
    /// Abstract base class representing a healthcare provider (Doctor or Nurse), derived from User (TPH).
    /// </summary>
    public abstract class Provider : User
    {
        // Professional info
        public string Specialty { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }

        // Verification & admin review
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
        public string? RejectionReason { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public int? VerifiedByAdminId { get; set; }
        public Admin? VerifiedByAdmin { get; set; }

        // Rating and financials
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public decimal WalletBalance { get; set; }

        // Location for search & distance filtering
        public string BaseCity { get; set; } = string.Empty;
        public double? BaseLatitude { get; set; }
        public double? BaseLongitude { get; set; }

        // Navigation properties
        public List<Booking> Bookings { get; set; } = new();
        public List<TimeSlot> TimeSlots { get; set; } = new();
        public List<ProviderCredential> ProviderCredentials { get; set; } = new();
        public List<ProviderService> ProviderServices { get; set; } = new();
    }
}

