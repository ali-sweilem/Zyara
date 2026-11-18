using System;
using Zyara.Services.Common;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Repositories.Data;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services.Features.Providers
{
    public class ProviderSearchService : IProviderSearchService
    {
        private const double BayesianM = 5.0;
        private const double BayesianC = 4.0;

        private readonly ZyaraDbContext _db;
        private readonly TimeProvider _timeProvider;

        public ProviderSearchService(ZyaraDbContext db, TimeProvider timeProvider)
        {
            _db = db;
            _timeProvider = timeProvider;
        }

        public async Task<PagedResult<ProviderCardDto>> SearchProvidersAsync(
            ProviderSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            var egyptNow = EgyptTime.Now(_timeProvider);
            var today = egyptNow.Date;
            var nowTime = egyptNow.TimeOfDay;

            // Base query: Only verified and active providers (Doctors and Nurses)
            var baseQuery = _db.Providers
                .AsNoTracking()
                .PubliclyVisible();

            // T13: location. Bounding box in SQL (cheap) when a radius is given; exact Haversine runs in memory below.
            var hasLocation = criteria.HasLocation;
            var lat = criteria.Lat ?? 0;
            var lng = criteria.Lng ?? 0;
            double? radiusKm = null;
            if (hasLocation && criteria.RadiusKm is > 0)
            {
                radiusKm = criteria.RadiusKm;
                baseQuery = baseQuery.WithinBoundingBox(lat, lng, radiusKm.Value);
            }
            var wantsDistanceSort = hasLocation &&
                string.Equals(criteria.SortBy?.Trim(), "distance", StringComparison.OrdinalIgnoreCase);

            // Project computed values ONCE into intermediate shape
            var intermediate = baseQuery.Select(p => new
            {
                Provider = p,
                MinFee = p.ProviderServices
                    .Where(ps => ps.Service == null || ps.Service.IsActive)
                    .Select(ps => (decimal?)ps.Fee)
                    .Min(),
                IsAvailable = p.TimeSlots.Any(ts =>
                    ts.Status == TimeSlotStatus.Available &&
                    (ts.Date > today || (ts.Date == today && ts.StartTime > nowTime))),
                WeightedRating = ((double)p.RatingCount / ((double)p.RatingCount + BayesianM)) * p.AverageRating
                                 + (BayesianM / ((double)p.RatingCount + BayesianM)) * BayesianC
            });

            // 1. Provider type is applied before search and pagination.
            if (criteria.ProviderType.HasValue)
            {
                var role = criteria.ProviderType.Value;
                intermediate = intermediate.Where(x => x.Provider.Role == role);
            }

            // 2. Search term (matches FullName, Specialty, BaseCity)
            if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
            {
                var term = criteria.SearchTerm.Trim();
                intermediate = intermediate.Where(x =>
                    x.Provider.FullName.Contains(term) ||
                    x.Provider.Specialty.Contains(term) ||
                    x.Provider.BaseCity.Contains(term));
            }

            // 3. Specialty
            if (!string.IsNullOrWhiteSpace(criteria.Specialty))
            {
                var specialty = criteria.Specialty.Trim();
                intermediate = intermediate.Where(x => x.Provider.Specialty == specialty);
            }

            // 4. City
            if (!string.IsNullOrWhiteSpace(criteria.City))
            {
                var city = criteria.City.Trim();
                intermediate = intermediate.Where(x => x.Provider.BaseCity == city);
            }

            // 5. Gender
            if (criteria.Gender.HasValue)
            {
                var gender = criteria.Gender.Value;
                intermediate = intermediate.Where(x => x.Provider.Gender == gender);
            }

            // 6. Minimum experience
            if (criteria.MinExperience.HasValue && criteria.MinExperience.Value > 0)
            {
                var minExp = criteria.MinExperience.Value;
                intermediate = intermediate.Where(x => x.Provider.ExperienceYears >= minExp);
            }

            // 7. Minimum rating (raw AverageRating)
            if (criteria.MinRating.HasValue && criteria.MinRating.Value > 0)
            {
                var minRating = criteria.MinRating.Value;
                intermediate = intermediate.Where(x => x.Provider.AverageRating >= minRating);
            }

            // 8. Price range
            if (criteria.MinPrice.HasValue)
            {
                var minPrice = criteria.MinPrice.Value;
                intermediate = intermediate.Where(x => x.MinFee != null && x.MinFee >= minPrice);
            }
            if (criteria.MaxPrice.HasValue)
            {
                var maxPrice = criteria.MaxPrice.Value;
                intermediate = intermediate.Where(x => x.MinFee != null && x.MinFee <= maxPrice);
            }

            // 9. Availability toggle
            if (criteria.AvailableOnly)
            {
                intermediate = intermediate.Where(x => x.IsAvailable);
            }

            var pageSize = criteria.PageSize > 0 ? criteria.PageSize : 12;

            // Card projection defined once (used by the normal path and by the distance path)
            var cardSelector = Project(intermediate, x => new ProviderCardDto
            {
                Id = x.Provider.Id,
                FullName = x.Provider.FullName,
                Role = x.Provider.Role,
                Specialty = x.Provider.Specialty,
                ExperienceYears = x.Provider.ExperienceYears,
                BaseCity = x.Provider.BaseCity,
                AverageRating = x.Provider.AverageRating,
                RatingCount = x.Provider.RatingCount,
                MinFee = x.MinFee,
                IsAvailable = x.IsAvailable,
                PhotoUrl = x.Provider.PhotoUrl
            });

            // 9.5 Location (T13): exact radius filter and/or "nearest first" ordering, done in memory
            if (hasLocation && (wantsDistanceSort || radiusKm.HasValue))
            {
                var rows = await intermediate
                    .Select(x => new { x.Provider.Id, x.Provider.BaseLatitude, x.Provider.BaseLongitude })
                    .ToListAsync(cancellationToken);

                var distances = new Dictionary<int, double>();
                var withoutGps = new List<int>();
                foreach (var r in rows)
                {
                    if (r.BaseLatitude is double pLat && r.BaseLongitude is double pLng &&
                        GeoDistance.IsValidCoordinate(pLat, pLng))
                    {
                        var d = GeoDistance.Haversine(lat, lng, pLat, pLng);
                        if (radiusKm is null || d <= radiusKm.Value)
                        {
                            distances[r.Id] = d;
                        }
                    }
                    else if (radiusKm is null)
                    {
                        withoutGps.Add(r.Id); // only keep them when there is no radius; they sort last
                    }
                }

                if (wantsDistanceSort)
                {
                    var orderedIds = distances
                        .OrderBy(kv => kv.Value).ThenBy(kv => kv.Key).Select(kv => kv.Key)
                        .Concat(withoutGps.OrderBy(i => i))
                        .ToList();

                    var size = Math.Clamp(pageSize, 1, 50);
                    var totalCount = orderedIds.Count;
                    var totalPages = (int)Math.Ceiling((double)totalCount / size);
                    var page = totalPages > 0 ? Math.Clamp(criteria.PageNumber, 1, totalPages) : 1;
                    var pageIds = orderedIds.Skip((page - 1) * size).Take(size).ToList();

                    var cards = pageIds.Count == 0
                        ? new List<ProviderCardDto>()
                        : await intermediate
                            .Where(x => pageIds.Contains(x.Provider.Id))
                            .Select(cardSelector)
                            .ToListAsync(cancellationToken);

                    var byId = cards.ToDictionary(c => c.Id);
                    var ordered = new List<ProviderCardDto>();
                    foreach (var id in pageIds)
                    {
                        if (!byId.TryGetValue(id, out var card)) continue;
                        if (distances.TryGetValue(id, out var km)) card.DistanceKm = Math.Round(km, 1);
                        ordered.Add(card);
                    }

                    return new PagedResult<ProviderCardDto>
                    {
                        Items = ordered,
                        TotalCount = totalCount,
                        PageNumber = page,
                        PageSize = size
                    };
                }

                // Radius only: keep the user's chosen sort (done in SQL) but only for providers inside the circle
                var insideIds = distances.Keys.ToList();
                intermediate = intermediate.Where(x => insideIds.Contains(x.Provider.Id));
            }

            // 10. Sorting
            switch (criteria.SortBy?.Trim().ToLowerInvariant())
            {
                case "rating":
                    intermediate = intermediate
                        .OrderByDescending(x => x.Provider.AverageRating)
                        .ThenByDescending(x => x.Provider.RatingCount)
                        .ThenBy(x => x.Provider.Id);
                    break;

                case "priceasc":
                    // Null prices sort last
                    intermediate = intermediate
                        .OrderBy(x => x.MinFee == null ? 1 : 0)
                        .ThenBy(x => x.MinFee)
                        .ThenBy(x => x.Provider.Id);
                    break;

                case "pricedesc":
                    // Null prices sort last
                    intermediate = intermediate
                        .OrderBy(x => x.MinFee == null ? 1 : 0)
                        .ThenByDescending(x => x.MinFee)
                        .ThenBy(x => x.Provider.Id);
                    break;

                case "experience":
                    intermediate = intermediate
                        .OrderByDescending(x => x.Provider.ExperienceYears)
                        .ThenBy(x => x.Provider.Id);
                    break;

                default:
                    // Business rule default: Available first, then WeightedRating desc, RatingCount desc, Id asc
                    intermediate = intermediate
                        .OrderByDescending(x => x.IsAvailable)
                        .ThenByDescending(x => x.WeightedRating)
                        .ThenByDescending(x => x.Provider.RatingCount)
                        .ThenBy(x => x.Provider.Id);
                    break;
            }

            var result = await intermediate.ToPagedAsync(
                cardSelector,
                criteria.PageNumber,
                pageSize,
                maxPageSize: 50,
                cancellationToken: cancellationToken);

            // T13: show the distance on the cards when the searcher shared a location
            if (hasLocation && result.Items.Count > 0)
            {
                await FillDistancesAsync(result.Items, lat, lng, cancellationToken);
            }

            return result;
        }

        // Lets us write the selector once for an anonymous element type.
        private static Expression<Func<T, ProviderCardDto>> Project<T>(
            IQueryable<T> _, Expression<Func<T, ProviderCardDto>> selector) => selector;

        private async Task FillDistancesAsync(
            IReadOnlyList<ProviderCardDto> items, double lat, double lng, CancellationToken cancellationToken)
        {
            var ids = items.Select(i => i.Id).ToList();
            var coords = await _db.Providers
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .Select(p => new { p.Id, p.BaseLatitude, p.BaseLongitude })
                .ToListAsync(cancellationToken);

            var byId = coords.ToDictionary(c => c.Id);
            foreach (var item in items)
            {
                if (byId.TryGetValue(item.Id, out var c) &&
                    c.BaseLatitude is double pLat && c.BaseLongitude is double pLng &&
                    GeoDistance.IsValidCoordinate(pLat, pLng))
                {
                    item.DistanceKm = Math.Round(GeoDistance.Haversine(lat, lng, pLat, pLng), 1);
                }
            }
        }

        public async Task<IReadOnlyList<string>> GetSpecialtiesAsync(UserRole providerType, CancellationToken cancellationToken = default)
        {
            return await _db.Providers
                .AsNoTracking()
                .PubliclyVisible()
                .Where(p => p.Role == providerType)
                .Where(p => p.Specialty != null && p.Specialty != "")
                .Select(p => p.Specialty.Trim())
                .Where(s => s != "")
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<string>> GetCitiesAsync(UserRole providerType, CancellationToken cancellationToken = default)
        {
            return await _db.Providers
                .AsNoTracking()
                .PubliclyVisible()
                .Where(p => p.Role == providerType)
                .Where(p => p.BaseCity != null && p.BaseCity != "")
                .Select(p => p.BaseCity.Trim())
                .Where(c => c != "")
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);
        }

    }
}


