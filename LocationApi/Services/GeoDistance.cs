namespace LocationApi.Services;

/// <summary>Tính khoảng cách trên mặt cầu theo công thức Haversine.</summary>
public static class GeoDistance
{
    private const double EarthRadiusKm = 6371.0088;

    public static double Kilometers(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return Math.Round(EarthRadiusKm * c, 2);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
