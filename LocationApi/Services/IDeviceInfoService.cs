using LocationApi.Models;

namespace LocationApi.Services;

/// <summary>Phân tích thông tin thiết bị/trình duyệt của client.</summary>
public interface IDeviceInfoService
{
    DeviceInfo Get(HttpContext context);
}
