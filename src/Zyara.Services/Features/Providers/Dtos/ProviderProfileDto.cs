using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Providers.Dtos
{
    public class ProviderProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public DateTime CreatedAt { get; set; }
        public String? Specialty { get; set; } 
        public int ExperienceYears { get; set; }
        public string? Bio { get; set; }

        public String BaseCity { get; set; } = string.Empty;
        public VerificationStatus VerificationStatus { get; set; }
        public String? RejectionReason { get; set; }
        public int RatingCount { get; set; }
        public decimal WalletBalance { get; set; }
        public int ServicesCount { get; set; }
        public int CredntialsCount { get; set; }
        public double AverageRating { get; set; }
        public String? PhotoUrl { get; set; }
        public IFormFile? PhotoFile { get; set; }



    }
}
