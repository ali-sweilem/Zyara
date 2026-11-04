using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Providers
{
    public class ProviderCardDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string Specialty { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public string BaseCity { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public decimal? MinFee { get; set; }
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Null for now until provider profile photo upload is implemented.
        /// </summary>
        public string? PhotoUrl { get; set; }
    }
}

