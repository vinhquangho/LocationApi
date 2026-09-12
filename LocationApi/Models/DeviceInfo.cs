namespace LocationApi.Models;

/// <summary>
/// Thông tin thiết bị/trình duyệt của client, suy ra từ header User-Agent
/// và User-Agent Client Hints (Sec-CH-UA-*).
/// </summary>
public sealed record DeviceInfo
{
    /// <summary>Chuỗi User-Agent nguyên bản.</summary>
    public string? UserAgent { get; init; }

    /// <summary>Loại thiết bị: Desktop, Mobile, Tablet, Bot hoặc Unknown.</summary>
    public string? DeviceType { get; init; }

    public string? DeviceFamily { get; init; }

    public string? DeviceBrand { get; init; }

    public string? DeviceModel { get; init; }

    public string? OperatingSystem { get; init; }

    public string? OperatingSystemVersion { get; init; }

    public string? Browser { get; init; }

    public string? BrowserVersion { get; init; }

    /// <summary>Engine hiển thị: Blink, WebKit, Gecko, Trident...</summary>
    public string? BrowserEngine { get; init; }

    public string? CpuArchitecture { get; init; }

    public bool IsMobile { get; init; }

    public bool IsTablet { get; init; }

    public bool IsDesktop { get; init; }

    public bool IsBot { get; init; }

    // ---- Dữ liệu bổ sung từ User-Agent Client Hints (trình duyệt Chromium gửi kèm) ----

    /// <summary>Nền tảng do trình duyệt khai báo (Sec-CH-UA-Platform), ví dụ "Windows", "Android".</summary>
    public string? Platform { get; init; }

    public string? PlatformVersion { get; init; }

    /// <summary>Danh sách thương hiệu/phiên bản trình duyệt từ Sec-CH-UA.</summary>
    public IReadOnlyList<string> ClientHintBrands { get; init; } = [];

    /// <summary>Ngôn ngữ ưu tiên đầu tiên của client.</summary>
    public string? PreferredLanguage { get; init; }

    /// <summary>Toàn bộ ngôn ngữ client chấp nhận (tách từ Accept-Language).</summary>
    public IReadOnlyList<string> AcceptLanguages { get; init; } = [];
}
