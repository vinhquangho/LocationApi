using System.Text.RegularExpressions;
using LocationApi.Models;
using Microsoft.Net.Http.Headers;
using UAParser;

namespace LocationApi.Services;

/// <summary>
/// Nhận diện thiết bị dựa trên thư viện UAParser (dữ liệu regex của ua-parser),
/// kết hợp với User-Agent Client Hints mà các trình duyệt Chromium gửi kèm.
/// </summary>
public sealed class UaParserDeviceInfoService : IDeviceInfoService
{
    private const string Unknown = "Unknown";

    /// <summary>Parser dùng chung; an toàn khi gọi đồng thời nên chỉ cần một instance.</summary>
    private static readonly Parser Parser = Parser.GetDefault();

    private static readonly Regex BotPattern = new(
        @"bot|crawler|spider|crawling|slurp|bingpreview|facebookexternalhit|headlesschrome|phantomjs|python-requests|curl|wget|httpclient",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NotABrandPattern = new(@"not.?a.?brand", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public DeviceInfo Get(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = context.Request.Headers;
        var userAgent = headers[HeaderNames.UserAgent].ToString();
        var hasUserAgent = !string.IsNullOrWhiteSpace(userAgent);
        var client = hasUserAgent ? Parser.Parse(userAgent) : null;

        var isBot = (client?.Device.IsSpider ?? false) || (hasUserAgent && BotPattern.IsMatch(userAgent));
        var deviceType = DetectDeviceType(client, headers, userAgent, isBot, hasUserAgent);

        var isMobile = string.Equals(deviceType, "Mobile", StringComparison.OrdinalIgnoreCase);
        var isTablet = string.Equals(deviceType, "Tablet", StringComparison.OrdinalIgnoreCase);
        var isDesktop = string.Equals(deviceType, "Desktop", StringComparison.OrdinalIgnoreCase);

        var acceptLanguages = ParseAcceptLanguages(headers[HeaderNames.AcceptLanguage].ToString());

        return new DeviceInfo
        {
            UserAgent = hasUserAgent ? userAgent : null,
            DeviceType = deviceType,
            DeviceFamily = NullIfUnknown(client?.Device.Family),
            DeviceBrand = NullIfUnknown(client?.Device.Brand),
            DeviceModel = NullIfUnknown(client?.Device.Model),
            OperatingSystem = NullIfUnknown(client?.OS.Family),
            OperatingSystemVersion = BuildVersion(client?.OS.Major, client?.OS.Minor, client?.OS.Patch),
            Browser = NullIfUnknown(client?.UA.Family),
            BrowserVersion = BuildVersion(client?.UA.Major, client?.UA.Minor, client?.UA.Patch),
            BrowserEngine = DetectEngine(client?.UA.Family),
            CpuArchitecture = ParseCpuArchitecture(userAgent),
            IsMobile = isMobile,
            IsTablet = isTablet,
            IsDesktop = isDesktop,
            IsBot = isBot,

            Platform = CleanClientHintValue(headers["Sec-CH-UA-Platform"].ToString()),
            PlatformVersion = CleanClientHintValue(headers["Sec-CH-UA-Platform-Version"].ToString()),
            ClientHintBrands = ParseClientHintBrands(headers["Sec-CH-UA"].ToString()),

            PreferredLanguage = acceptLanguages.FirstOrDefault(),
            AcceptLanguages = acceptLanguages
        };
    }

    private static string DetectDeviceType(
        ClientInfo? client,
        IHeaderDictionary headers,
        string userAgent,
        bool isBot,
        bool hasUserAgent)
    {
        if (isBot)
        {
            return "Bot";
        }

        if (!hasUserAgent)
        {
            return Unknown;
        }

        // Client Hints đáng tin cậy hơn User-Agent khi có mặt.
        var mobileHint = headers["Sec-CH-UA-Mobile"].ToString();
        if (mobileHint.Contains("?1", StringComparison.Ordinal))
        {
            return "Mobile";
        }

        var family = client?.Device.Family ?? string.Empty;

        if (family.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            family.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ||
            family.Contains("Nexus 7", StringComparison.OrdinalIgnoreCase) ||
            family.Contains("Nexus 10", StringComparison.OrdinalIgnoreCase))
        {
            return "Tablet";
        }

        if (family.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
            family.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Mobi", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            // Android không kèm "Mobile" thường là tablet.
            if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) &&
                !userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) &&
                !family.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
            {
                return "Tablet";
            }

            return "Mobile";
        }

        return "Desktop";
    }

    private static string? ParseCpuArchitecture(string userAgent)
    {
        if (userAgent.Contains("arm64", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("aarch64", StringComparison.OrdinalIgnoreCase))
        {
            return "ARM64";
        }

        if (userAgent.Contains("x86_64", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("x64", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Win64", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("WOW64", StringComparison.OrdinalIgnoreCase))
        {
            return "x64";
        }

        if (userAgent.Contains("arm", StringComparison.OrdinalIgnoreCase))
        {
            return "ARM";
        }

        if (userAgent.Contains("x86", StringComparison.OrdinalIgnoreCase))
        {
            return "x86";
        }

        return null;
    }

    private static string? DetectEngine(string? browserFamily)
    {
        if (string.IsNullOrWhiteSpace(browserFamily) || browserFamily == Unknown)
        {
            return null;
        }

        return browserFamily switch
        {
            "Edge" or "Chrome" or "Chromium" or "Opera" or "Samsung Internet" or "Vivaldi" or "Brave"
                or "Electron" or "Electron Framework" => "Blink",
            "Safari" or "Mobile Safari" or "iOS Safari" => "WebKit",
            "Firefox" or "Firefox Mobile" => "Gecko",
            "IE" or "Internet Explorer" => "Trident",
            _ => null
        };
    }

    private static IReadOnlyList<string> ParseClientHintBrands(string headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return [];
        }

        // Ví dụ: "Chromium";v="124", "Google Chrome";v="124", "Not-A.Brand";v="99"
        return headerValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Trim().Trim('"'))
            .Where(part => part.Length > 0 && !NotABrandPattern.IsMatch(part))
            .Select(part => part.Replace("\"", string.Empty, StringComparison.Ordinal))
            .ToList();
    }

    private static IReadOnlyList<string> ParseAcceptLanguages(string headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return [];
        }

        return headerValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item =>
            {
                var semicolon = item.IndexOf(';');
                return (semicolon > 0 ? item[..semicolon] : item).Trim();
            })
            .Where(item => item.Length > 0)
            .ToList();
    }

    private static string? CleanClientHintValue(string value)
    {
        var cleaned = value.Trim().Trim('"');
        return cleaned.Length == 0 ? null : cleaned;
    }

    private static string? BuildVersion(string? major, string? minor, string? patch)
    {
        // Bỏ các phần rỗng, nhưng vẫn giữ "0" ở giữa để version đúng định dạng (ví dụ 124.0.6367).
        var parts = new[] { major, minor, patch }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .ToArray();

        return parts.Length == 0 ? null : string.Join('.', parts);
    }

    private static string? NullIfUnknown(string? value)
        => string.IsNullOrWhiteSpace(value) || value == Unknown || value == "Other" ? null : value;
}
