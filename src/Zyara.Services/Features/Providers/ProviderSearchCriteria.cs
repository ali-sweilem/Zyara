using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Providers
{
    public class ProviderSearchCriteria
    {
        public string? SearchTerm { get; set; }
        public UserRole? ProviderType { get; set; }
        public string? Specialty { get; set; }
        public string? City { get; set; }
        public Gender? Gender { get; set; }
        public int? MinExperience { get; set; }
        public double? MinRating { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool AvailableOnly { get; set; }
        public string? SortBy { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}

