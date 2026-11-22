using Xunit;
using Zyara.Domain.Rules;

namespace Zyara.Tests;

public class GeoDistanceTests
{
    private const double CairoLat = 30.0444, CairoLon = 31.2357;
    private const double AlexLat = 31.2001, AlexLon = 29.9187;

    [Fact]
    public void Cairo_to_Alexandria_is_about_180_km()
    {
        var km = GeoDistance.Haversine(CairoLat, CairoLon, AlexLat, AlexLon);
        Assert.InRange(km, 178, 182);
    }

    [Fact]
    public void Same_point_is_zero()
    {
        Assert.Equal(0, GeoDistance.Haversine(CairoLat, CairoLon, CairoLat, CairoLon), 6);
    }

    [Fact]
    public void Distance_is_symmetric()
    {
        var a = GeoDistance.Haversine(CairoLat, CairoLon, AlexLat, AlexLon);
        var b = GeoDistance.Haversine(AlexLat, AlexLon, CairoLat, CairoLon);
        Assert.Equal(a, b, 9);
    }

    [Fact]
    public void One_degree_of_latitude_is_about_111_km()
    {
        Assert.InRange(GeoDistance.Haversine(0, 0, 1, 0), 111.0, 111.4);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public void Invalid_coordinates_are_rejected(double lat, double lon)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GeoDistance.Haversine(lat, lon, 0, 0));
    }

    [Fact]
    public void BoundingBox_contains_every_point_on_the_circle()
    {
        var box = GeoDistance.BoundingBoxFor(CairoLat, CairoLon, 50);
        // A point ~40 km north of Cairo must be inside the box.
        Assert.InRange(CairoLat + 0.36, box.MinLat, box.MaxLat);
        Assert.InRange(CairoLon, box.MinLon, box.MaxLon);
        // Alexandria (180 km) must be outside a 50 km box.
        Assert.False(AlexLat >= box.MinLat && AlexLat <= box.MaxLat && AlexLon >= box.MinLon && AlexLon <= box.MaxLon);
    }

    [Fact]
    public void BoundingBox_rejects_non_positive_radius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GeoDistance.BoundingBoxFor(CairoLat, CairoLon, 0));
    }
}
