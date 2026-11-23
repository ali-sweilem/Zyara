using Zyara.Domain.Enums;
using Zyara.Domain.Rules;

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

        // T13: search near a point. SortBy = "distance" sorts by nearest; RadiusKm limits the results.
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public double? RadiusKm { get; set; }

        public bool HasLocation =>
            Lat.HasValue && Lng.HasValue && GeoDistance.IsValidCoordinate(Lat.Value, Lng.Value);

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}


