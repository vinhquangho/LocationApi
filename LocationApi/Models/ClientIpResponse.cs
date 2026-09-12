namespace LocationApi.Models;

/// <summary>Kết quả của endpoint chỉ lấy địa chỉ IP.</summary>
public sealed record ClientIpResponse
{
    public required string Ip { get; init; }

    /// <summary>True nếu là IP công khai (có thể tra cứu vị trí trên Internet).</summary>
    public bool IsPublic { get; init; }

    public string? Note { get; init; }
}
