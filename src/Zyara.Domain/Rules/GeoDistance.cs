namespace Zyara.Domain.Rules;

/// <summary>Pure geo helpers (no DB, no clock). T13.</summary>
public static class GeoDistance
{
    public const double EarthRadiusKm = 6371.0088;

    public readonly record struct BoundingBox(double MinLat, double MaxLat, double MinLon, double MaxLon);

    public static bool IsValidCoordinate(double lat, double lon) =>
        !double.IsNaN(lat) && !double.IsNaN(lon) &&
        lat is >= -90 and <= 90 && lon is >= -180 and <= 180;

    /// <summary>Great-circle distance in kilometres.</summary>
    public static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        if (!IsValidCoordinate(lat1, lon1)) throw new ArgumentOutOfRangeException(nameof(lat1), "Invalid coordinate.");
        if (!IsValidCoordinate(lat2, lon2)) throw new ArgumentOutOfRangeException(nameof(lat2), "Invalid coordinate.");

        var phi1 = ToRadians(lat1);
        var phi2 = ToRadians(lat2);
        var dPhi = ToRadians(lat2 - lat1);
        var dLambda = ToRadians(lon2 - lon1);

        var a = Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dLambda / 2) * Math.Sin(dLambda / 2);

        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>
    /// Cheap rectangle that contains the whole circle (used in SQL before the exact Haversine check).
    /// Near the poles / antimeridian it widens to the full range (still correct, just less selective).
    /// </summary>
    public static BoundingBox BoundingBoxFor(double lat, double lon, double radiusKm)
    {
        if (!IsValidCoordinate(lat, lon)) throw new ArgumentOutOfRangeException(nameof(lat), "Invalid coordinate.");
        if (radiusKm <= 0) throw new ArgumentOutOfRangeException(nameof(radiusKm));

        var latDelta = ToDegrees(radiusKm / EarthRadiusKm);
        var minLat = Math.Max(-90, lat - latDelta);
        var maxLat = Math.Min(90, lat + latDelta);

        var cos = Math.Cos(ToRadians(lat));
        double minLon = -180, maxLon = 180;
        if (cos > 1e-6)
        {
            var lonDelta = latDelta / cos;
            if (lon - lonDelta >= -180 && lon + lonDelta <= 180)
            {
                minLon = lon - lonDelta;
                maxLon = lon + lonDelta;
            }
        }

        return new BoundingBox(minLat, maxLat, minLon, maxLon);
    }

    private static double ToRadians(double deg) => deg * Math.PI / 180.0;
    private static double ToDegrees(double rad) => rad * 180.0 / Math.PI;
}
