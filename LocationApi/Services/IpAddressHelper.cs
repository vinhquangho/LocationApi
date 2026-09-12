using System.Net;
using System.Net.Sockets;

namespace LocationApi.Services;

/// <summary>Tiện ích xử lý và phân loại địa chỉ IP.</summary>
public static class IpAddressHelper
{
    /// <summary>
    /// Chuẩn hoá chuỗi IP: bỏ khoảng trắng, bỏ cổng (dạng "1.2.3.4:5678"),
    /// chuyển IPv4-mapped IPv6 (::ffff:1.2.3.4) về IPv4.
    /// Trả về null nếu chuỗi không phải là IP hợp lệ.
    /// </summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var candidate = value.Trim().Trim('"');

        // Một số proxy trả về dạng "1.2.3.4:5678" hoặc "[::1]:5678".
        if (candidate.StartsWith('['))
        {
            var closing = candidate.IndexOf(']');
            if (closing > 0)
            {
                candidate = candidate[1..closing];
            }
        }
        else if (candidate.Count(c => c == ':') == 1)
        {
            candidate = candidate[..candidate.IndexOf(':')];
        }

        if (!IPAddress.TryParse(candidate, out var address))
        {
            return null;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.ToString();
    }

    /// <summary>True nếu chuỗi là IP công khai (không thuộc dải nội bộ/loopback/link-local).</summary>
    public static bool IsPublic(string? ip)
        => IPAddress.TryParse(ip, out var address) && IsPublic(address);

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return !IsPrivateOrLoopback(address);
    }

    /// <summary>True nếu IP thuộc dải riêng tư (RFC 1918), loopback, link-local hoặc unique local IPv6.</summary>
    public static bool IsPrivateOrLoopback(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] switch
            {
                10 => true,                       // 10.0.0.0/8
                127 => true,                      // 127.0.0.0/8
                169 when bytes[1] == 254 => true, // 169.254.0.0/16 (link-local)
                172 when bytes[1] is >= 16 and <= 31 => true, // 172.16.0.0/12
                192 when bytes[1] == 168 => true, // 192.168.0.0/16
                0 => true,                        // 0.0.0.0/8
                _ => false
            };
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
            {
                return true;
            }

            // fc00::/7 - Unique Local Address
            return (bytes[0] & 0xFE) == 0xFC;
        }

        return true;
    }
}
