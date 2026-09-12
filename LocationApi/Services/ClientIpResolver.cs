namespace LocationApi.Services;

/// <summary>
/// Đọc IP client từ các header do proxy/CDN gửi tới, sau đó mới tới
/// <see cref="HttpContext.Connection"/>.
/// </summary>
/// <remarks>
/// Thứ tự header được chọn theo mức độ tin cậy của nhà cung cấp:
/// Cloudflare (CF-Connecting-IP) -> True-Client-IP -> X-Real-IP -> X-Forwarded-For.
/// LƯU Ý: các header này do client có thể tự gửi, nên chỉ nên tin tưởng khi
/// ứng dụng thực sự nằm sau một reverse proxy đã cấu hình ghi đè header.
/// </remarks>
public sealed class ClientIpResolver : IClientIpResolver
{
    private static readonly string[] TrustedHeaders =
    [
        "CF-Connecting-IP",
        "True-Client-IP",
        "X-Real-IP",
        "X-Forwarded-For",
        "X-Client-IP"
    ];

    private const string FallbackIp = "0.0.0.0";

    public string Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var headerName in TrustedHeaders)
        {
            if (!context.Request.Headers.TryGetValue(headerName, out var values))
            {
                continue;
            }

            // X-Forwarded-For có dạng "client, proxy1, proxy2" -> phần tử đầu tiên là client.
            var firstValue = values
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            var normalized = IpAddressHelper.Normalize(firstValue);
            if (normalized is not null)
            {
                return normalized;
            }
        }

        return IpAddressHelper.Normalize(context.Connection.RemoteIpAddress?.ToString()) ?? FallbackIp;
    }

    public bool IsPublic(string? ip) => IpAddressHelper.IsPublic(ip);
}
