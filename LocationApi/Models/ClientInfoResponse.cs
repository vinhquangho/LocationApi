namespace LocationApi.Models;

/// <summary>Kết quả tổng hợp: IP + vị trí + thiết bị + thông tin yêu cầu.</summary>
public sealed record ClientInfoResponse
{
    /// <summary>Địa chỉ IP của client đang truy cập.</summary>
    public required string Ip { get; init; }

    /// <summary>True nếu IP là IP công khai trên Internet.</summary>
    public bool IsPublicIp { get; init; }

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Vị trí hiện tại ước lượng theo địa chỉ IP.</summary>
    public GeoLocationInfo Location { get; init; } = new();

    /// <summary>Thông tin thiết bị/trình duyệt.</summary>
    public DeviceInfo Device { get; init; } = new();

    /// <summary>Thông tin về chính HTTP request.</summary>
    public RequestInfo Request { get; init; } = new();
}
