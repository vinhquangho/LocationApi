using System.Globalization;

namespace LocationApi.Models;

/// <summary>
/// Vị trí hiện tại (ước lượng) của client, được suy ra từ địa chỉ IP.
/// </summary>
public sealed record GeoLocationInfo
{
    /// <summary>Tra cứu thành công hay không.</summary>
    public bool Success { get; init; }

    /// <summary>Địa chỉ IP của client (địa chỉ nhìn thấy được từ phía server).</summary>
    public string? ClientIp { get; init; }

    /// <summary>Địa chỉ IP thực tế đã dùng để tra cứu vị trí.</summary>
    public string? LookupIp { get; init; }

    /// <summary>
    /// True khi client dùng IP nội bộ/loopback (ví dụ chạy localhost) nên API phải dùng
    /// IP công khai của máy chủ để tra cứu. Khi đó kết quả chỉ mang tính chất tham khảo.
    /// </summary>
    public bool ResolvedUsingServerPublicIp { get; init; }

    /// <summary>Loại địa chỉ IP: IPv4 hoặc IPv6.</summary>
    public string? IpType { get; init; }

    public string? Continent { get; init; }

    public string? Country { get; init; }

    public string? CountryCode { get; init; }

    public string? Region { get; init; }

    public string? RegionCode { get; init; }

    public string? City { get; init; }

    public string? PostalCode { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public string? TimeZone { get; init; }

    public string? Isp { get; init; }

    public string? Organization { get; init; }

    public string? Asn { get; init; }

    /// <summary>Nhà cung cấp dịch vụ tra cứu đã dùng.</summary>
    public string? Provider { get; init; }

    /// <summary>Thông báo lỗi (nếu tra cứu thất bại).</summary>
    public string? Error { get; init; }

    /// <summary>
    /// Giới hạn độ chính xác của phương pháp định vị theo IP. Định vị theo IP
    /// không thể chính xác tới mức địa chỉ nhà.
    /// </summary>
    public string Accuracy => "Theo IP: chính xác tới mức thành phố (~5-50 km)";

    public string? GoogleMapsUrl => HasCoordinates
        ? $"https://www.google.com/maps?q={Format(Latitude!.Value)},{Format(Longitude!.Value)}"
        : null;

    public string? OpenStreetMapUrl => HasCoordinates
        ? $"https://www.openstreetmap.org/?mlat={Format(Latitude!.Value)}&mlon={Format(Longitude!.Value)}#map=13/{Format(Latitude!.Value)}/{Format(Longitude!.Value)}"
        : null;

    private bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
