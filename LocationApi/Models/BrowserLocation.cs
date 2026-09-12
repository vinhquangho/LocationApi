using System.Globalization;

namespace LocationApi.Models;

/// <summary>Toạ độ GPS chính xác do trình duyệt (navigator.geolocation) cung cấp.</summary>
public sealed record BrowserLocation
{
    public double Latitude { get; init; }

    public double Longitude { get; init; }

    /// <summary>Bán kính sai số (mét) do thiết bị báo về.</summary>
    public double? AccuracyMeters { get; init; }

    public double? AltitudeMeters { get; init; }

    /// <summary>Nguồn dữ liệu, ví dụ "browser-geolocation".</summary>
    public string? Source { get; init; }

    public DateTimeOffset ReceivedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string GoogleMapsUrl =>
        $"https://www.google.com/maps?q={Latitude.ToString(CultureInfo.InvariantCulture)},{Longitude.ToString(CultureInfo.InvariantCulture)}";
}
