using LocationApi.Models;

namespace LocationApi.Services;

/// <summary>Tra cứu vị trí địa lý (ước lượng) từ địa chỉ IP.</summary>
public interface IGeoLocationService
{
    Task<GeoLocationInfo> GetAsync(string clientIp, CancellationToken cancellationToken = default);
}
