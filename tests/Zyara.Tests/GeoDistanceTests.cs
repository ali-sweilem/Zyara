using Zyara.Domain.Rules;

namespace Zyara.Tests;

public class GeoDistanceTests
{
    // ===== Haversine =====

    [Fact]
    public void Same_point_is_zero()
    {
        Assert.Equal(0, GeoDistance.Haversine(30.0444, 31.2357, 30.0444, 31.2357), 6);
    }

    [Fact]
    public void Cairo_to_Alexandria_is_about_180_km()
    {
        var km = GeoDistance.Haversine(30.0444, 31.2357, 31.2001, 29.9187);
        Assert.InRange(km, 175, 185);
    }

    [Fact]
    public void Distance_is_symmetric()
    {
        var ab = GeoDistance.Haversine(30.0444, 31.2357, 31.2001, 29.9187);
        var ba = GeoDistance.Haversine(31.2001, 29.9187, 30.0444, 31.2357);
        Assert.Equal(ab, ba, 9);
    }

    [Fact]
    public void One_degree_of_latitude_is_about_111_km()
    {
        Assert.Equal(111.19, GeoDistance.Haversine(0, 0, 1, 0), 1);
    }

    [Fact]
    public void Opposite_sides_of_the_earth_are_half_a_circumference_apart()
    {
        Assert.Equal(20015.1, GeoDistance.Haversine(0, 0, 0, 180), 0);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    [InlineData(double.NaN, 0)]
    public void Invalid_coordinates_are_rejected(double lat, double lon)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GeoDistance.Haversine(lat, lon, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GeoDistance.Haversine(0, 0, lat, lon));
    }

    [Fact]
    public void IsValidCoordinate_accepts_the_edges_and_rejects_NaN()
    {
        Assert.True(GeoDistance.IsValidCoordinate(90, 180));
        Assert.True(GeoDistance.IsValidCoordinate(-90, -180));
        Assert.False(GeoDistance.IsValidCoordinate(double.NaN, 0));
        Assert.False(GeoDistance.IsValidCoordinate(0, double.NaN));
    }

    // ===== Bounding box =====

    [Fact]
    public void Bounding_box_contains_points_inside_the_radius()
    {
        const double lat = 30.0444, lon = 31.2357, radius = 50;
        var box = GeoDistance.BoundingBoxFor(lat, lon, radius);

        // A point 40 km north and one 40 km east (roughly) must be inside the box.
        var north = lat + 40 / 111.19;
        var east = lon + 40 / (111.19 * Math.Cos(lat * Math.PI / 180));

        Assert.InRange(north, box.MinLat, box.MaxLat);
        Assert.InRange(east, box.MinLon, box.MaxLon);
    }

    [Fact]
    public void Bounding_box_excludes_far_away_points()
    {
        var box = GeoDistance.BoundingBoxFor(30.0444, 31.2357, 10);

        // Alexandria is ~180 km away.
        Assert.False(31.2001 >= box.MinLat && 31.2001 <= box.MaxLat &&
                     29.9187 >= box.MinLon && 29.9187 <= box.MaxLon);
    }

    [Fact]
    public void Bounding_box_is_centred_on_the_point()
    {
        var box = GeoDistance.BoundingBoxFor(10, 20, 100);
        Assert.Equal(10, (box.MinLat + box.MaxLat) / 2, 9);
        Assert.Equal(20, (box.MinLon + box.MaxLon) / 2, 9);
    }

    [Fact]
    public void Bounding_box_widens_to_all_longitudes_at_the_pole()
    {
        var box = GeoDistance.BoundingBoxFor(90, 0, 100);
        Assert.Equal(-180, box.MinLon);
        Assert.Equal(180, box.MaxLon);
        Assert.Equal(90, box.MaxLat);
    }

    [Fact]
    public void Bounding_box_widens_across_the_antimeridian()
    {
        var box = GeoDistance.BoundingBoxFor(0, 179.9, 100);
        Assert.Equal(-180, box.MinLon);
        Assert.Equal(180, box.MaxLon);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Bounding_box_needs_a_positive_radius(double radius)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GeoDistance.BoundingBoxFor(0, 0, radius));
    }

    [Fact]
    public void Bounding_box_rejects_an_invalid_centre()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GeoDistance.BoundingBoxFor(95, 0, 10));
    }
}
