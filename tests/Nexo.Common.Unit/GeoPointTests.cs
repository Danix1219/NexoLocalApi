using Nexo.Common.Geo;

namespace Nexo.Common.Unit;

public sealed class GeoPointTests
{
    [Fact]
    public void GeoJson_uses_longitude_then_latitude()
    {
        var point = new GeoPoint(-34.6037, -58.3816);

        Assert.Equal(new[] { -58.3816, -34.6037 }, point.ToGeoJson());
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public void Rejects_out_of_range_coordinates(double latitude, double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(latitude, longitude));
    }
}
