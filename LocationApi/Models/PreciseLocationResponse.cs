namespace LocationApi.Models;

/// <summary>Kết quả đối chiếu giữa toạ độ GPS của trình duyệt và vị trí ước lượng theo IP.</summary>
public sealed record PreciseLocationResponse
{
    public bool Success { get; init; }

    public string? Error { get; init; }

    /// <summary>Toạ độ chính xác do trình duyệt cung cấp.</summary>
    public BrowserLocation? BrowserLocation { get; init; }

    /// <summary>Vị trí ước lượng theo IP để so sánh.</summary>
    public GeoLocationInfo? IpLocation { get; init; }

    /// <summary>Khoảng cách (km) giữa toạ độ GPS và tâm vị trí theo IP.</summary>
    public double? DistanceFromIpLocationKm { get; init; }
}
