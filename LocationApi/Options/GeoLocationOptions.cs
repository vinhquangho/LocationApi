namespace LocationApi.Options;

/// <summary>
/// Cấu hình cho dịch vụ tra cứu vị trí theo địa chỉ IP.
/// Được đọc từ section "GeoLocation" trong appsettings.json.
/// </summary>
public sealed class GeoLocationOptions
{
    public const string SectionName = "GeoLocation";

    /// <summary>Địa chỉ gốc của dịch vụ tra cứu. Mặc định dùng ipwho.is (miễn phí, không cần API key).</summary>
    public string BaseUrl { get; set; } = "https://ipwho.is";

    /// <summary>Dịch vụ dùng để lấy IP công khai của máy chủ khi client là IP nội bộ/loopback.</summary>
    public string PublicIpLookupUrl { get; set; } = "https://api.ipify.org?format=json";

    /// <summary>Thời gian timeout cho mỗi lần gọi dịch vụ bên ngoài (giây).</summary>
    public int TimeoutSeconds { get; set; } = 8;

    /// <summary>Thời gian cache kết quả tra cứu thành công (phút).</summary>
    public int CacheMinutes { get; set; } = 30;
}
