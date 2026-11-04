using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Data;

namespace Zyara.Services.Features.Providers;

public sealed class ProviderDetailsService : IProviderDetailsService
{
    private readonly ZyaraDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ProviderDetailsService(ZyaraDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<ProviderDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var egyptNow = EgyptTime.Now(_timeProvider);
        var today = egyptNow.Date;
        var nowTime = egyptNow.TimeOfDay;

        var provider = await _db.Providers
            .AsNoTracking()
            .PubliclyVisible()
            .Where(p => p.Id == id)
            .AsSplitQuery()
            .Select(p => new ProviderDetailsDto
            {
                Id = p.Id,
                FullName = p.FullName,
                Role = p.Role,
                Specialty = p.Specialty,
                Bio = p.Bio,
                ExperienceYears = p.ExperienceYears,
                BaseCity = p.BaseCity,
                Gender = p.Gender,
                AverageRating = p.AverageRating,
                RatingCount = p.RatingCount,
                PhotoUrl = p.PhotoUrl,
                MinFee = p.ProviderServices
                    .Where(ps => ps.Service != null && ps.Service.IsActive)
                    .Select(ps => (decimal?)ps.Fee)
                    .Min(),
                Services = p.ProviderServices
                    .Where(ps => ps.Service != null && ps.Service.IsActive)
                    .OrderBy(ps => ps.Fee)
                    .Select(ps => new ProviderServiceItemDto
                    {
                        Id = ps.Id,
                        Name = ps.Service!.Name,
                        Description = ps.Service.Description,
                        Fee = ps.Fee
                    })
                    .ToList(),
                UpcomingSlots = p.TimeSlots
                    .Where(ts => ts.Status == TimeSlotStatus.Available &&
                        (ts.Date > today || (ts.Date == today && ts.StartTime > nowTime)))
                    .OrderBy(ts => ts.Date)
                    .ThenBy(ts => ts.StartTime)
                    .Take(30)
                    .Select(ts => new UpcomingSlotDto
                    {
                        Id = ts.Id,
                        Date = ts.Date,
                        StartTime = ts.StartTime,
                        EndTime = ts.EndTime
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (provider is not null)
        {
            provider.IsAvailable = provider.UpcomingSlots.Count > 0;
        }

        return provider;
    }

    public async Task<PagedResult<ProviderReviewDto>> GetReviewsAsync(
        int providerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber > 0 ? pageNumber : 1;
        pageSize = Math.Clamp(pageSize > 0 ? pageSize : 5, 1, 20);

        var query = _db.Reviews
            .AsNoTracking()
            .Where(r => r.Booking.ProviderId == providerId)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                ReviewerName = r.Booking.Patient!.FullName,
                Rating = r.Stars,
                r.Comment,
                r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new ProviderReviewDto
        {
            ReviewerName = MaskReviewerName(r.ReviewerName),
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToList();

        return new PagedResult<ProviderReviewDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static string MaskReviewerName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "?";
        }

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return parts[0];
        }

        return $"{parts[0]} {parts[1][..1]}.";
    }
}
