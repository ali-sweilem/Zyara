using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
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

            // Execute single Count query before Skip/Take
            var totalCount = await intermediate.CountAsync(cancellationToken);

            var pageNumber = criteria.PageNumber > 0 ? criteria.PageNumber : 1;
            var pageSize = Math.Clamp(criteria.PageSize > 0 ? criteria.PageSize : 12, 1, 50);

            // Execute Skip/Take in database and project directly to ProviderCardDto
            var items = await intermediate
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProviderCardDto
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
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<ProviderCardDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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

