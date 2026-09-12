namespace LocationApi.Services;

/// <summary>Xác định địa chỉ IP thật của client khi ứng dụng chạy sau proxy/CDN.</summary>
public interface IClientIpResolver
{
    /// <summary>Trả về địa chỉ IP của client (đã chuẩn hoá, không kèm cổng).</summary>
    string Resolve(HttpContext context);

    /// <summary>True nếu IP là IP công khai trên Internet.</summary>
    bool IsPublic(string? ip);
}
