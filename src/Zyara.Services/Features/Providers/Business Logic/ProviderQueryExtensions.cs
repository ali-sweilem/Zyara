using System.Linq;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;

namespace Zyara.Services.Features.Providers;

internal static class ProviderQueryExtensions
{
    public static IQueryable<Provider> PubliclyVisible(this IQueryable<Provider> query) =>
        query.Where(p => p.VerificationStatus == VerificationStatus.Verified && p.IsActive);

    /// <summary>
    /// T13: cheap SQL pre-filter (rectangle around the point). Providers without GPS are excluded.
    /// The exact Haversine check happens in memory afterwards.
    /// </summary>
    public static IQueryable<Provider> WithinBoundingBox(
        this IQueryable<Provider> query, double lat, double lng, double radiusKm)
    {
        var box = GeoDistance.BoundingBoxFor(lat, lng, radiusKm);
        var minLat = box.MinLat;
        var maxLat = box.MaxLat;
        var minLon = box.MinLon;
        var maxLon = box.MaxLon;

        return query.Where(p =>
            p.BaseLatitude != null && p.BaseLongitude != null &&
            p.BaseLatitude >= minLat && p.BaseLatitude <= maxLat &&
            p.BaseLongitude >= minLon && p.BaseLongitude <= maxLon);
    }
}
