using System.Text.Json;
using System.Text.Json.Serialization;
using LocationApi.Models;
using LocationApi.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LocationApi.Services;

/// <summary>
/// Tra cứu vị trí theo IP thông qua dịch vụ công khai ipwho.is
/// (miễn phí, không cần API key, hỗ trợ HTTPS).
/// </summary>
public sealed class IpWhoIsGeoLocationService : IGeoLocationService
{
    private const string ProviderName = "ipwho.is";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private static readonly TimeSpan FailureCacheDuration = TimeSpan.FromMinutes(1);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IClientIpResolver _ipResolver;
    private readonly ILogger<IpWhoIsGeoLocationService> _logger;
    private readonly GeoLocationOptions _options;

    public IpWhoIsGeoLocationService(
        HttpClient httpClient,
        IMemoryCache cache,
        IClientIpResolver ipResolver,
        IOptions<GeoLocationOptions> options,
        ILogger<IpWhoIsGeoLocationService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _ipResolver = ipResolver;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<GeoLocationInfo> GetAsync(string clientIp, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"geo:v1:{clientIp}";

        if (_cache.TryGetValue(cacheKey, out GeoLocationInfo? cached) && cached is not null)
        {
            _logger.LogDebug("Dùng lại kết quả tra cứu vị trí đã cache cho IP {Ip}", clientIp);
            return cached;
        }

        var result = await LookupAsync(clientIp, cancellationToken).ConfigureAwait(false);

        var cacheDuration = result.Success
            ? TimeSpan.FromMinutes(Math.Clamp(_options.CacheMinutes, 1, 1440))
            : FailureCacheDuration;

        _cache.Set(cacheKey, result, cacheDuration);

        return result;
    }

    private async Task<GeoLocationInfo> LookupAsync(string clientIp, CancellationToken cancellationToken)
    {
        var lookupIp = clientIp;
        var usedServerPublicIp = false;

        if (!_ipResolver.IsPublic(clientIp))
        {
            // Khi chạy localhost (127.0.0.1 / ::1) hoặc trong mạng LAN, IP không thể tra cứu được.
            // Giải pháp hữu ích: lấy IP công khai của máy chủ rồi tra cứu IP đó.
            var serverPublicIp = await GetServerPublicIpAsync(cancellationToken).ConfigureAwait(false);

            if (serverPublicIp is null)
            {
                return Failure(
                    clientIp,
                    clientIp,
                    usedServerPublicIp: false,
                    "Client đang dùng địa chỉ IP nội bộ hoặc loopback nên không thể tra cứu vị trí.");
            }

            lookupIp = serverPublicIp;
            usedServerPublicIp = true;
        }

        try
        {
            var url = $"{_options.BaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(lookupIp)}";
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Dịch vụ {Provider} trả về mã {StatusCode} cho IP {Ip}",
                    ProviderName,
                    (int)response.StatusCode,
                    lookupIp);

                return Failure(
                    clientIp,
                    lookupIp,
                    usedServerPublicIp,
                    $"Dịch vụ tra cứu vị trí trả về mã HTTP {(int)response.StatusCode}.");
            }

            var payload = await response.Content
                .ReadFromJsonAsync<IpWhoIsResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (payload is null)
            {
                return Failure(clientIp, lookupIp, usedServerPublicIp, "Không đọc được dữ liệu trả về từ dịch vụ tra cứu vị trí.");
            }

            if (!payload.Success)
            {
                return Failure(
                    clientIp,
                    lookupIp,
                    usedServerPublicIp,
                    payload.Message ?? "Không tra cứu được vị trí cho địa chỉ IP này.");
            }

            return new GeoLocationInfo
            {
                Success = true,
                ClientIp = clientIp,
                LookupIp = payload.Ip ?? lookupIp,
                ResolvedUsingServerPublicIp = usedServerPublicIp,
                IpType = Clean(payload.Type),
                Continent = Clean(payload.Continent),
                Country = Clean(payload.Country),
                CountryCode = Clean(payload.CountryCode),
                Region = Clean(payload.Region),
                RegionCode = Clean(payload.RegionCode),
                City = Clean(payload.City),
                PostalCode = Clean(payload.Postal),
                Latitude = payload.Latitude,
                Longitude = payload.Longitude,
                TimeZone = Clean(payload.Timezone?.Id),
                Isp = Clean(payload.Connection?.Isp),
                Organization = Clean(payload.Connection?.Org),
                Asn = payload.Connection?.Asn?.ToString(),
                Provider = ProviderName
            };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Hết thời gian chờ khi tra cứu vị trí cho IP {Ip}", lookupIp);
            return Failure(clientIp, lookupIp, usedServerPublicIp, "Hết thời gian chờ khi gọi dịch vụ tra cứu vị trí.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi mạng khi tra cứu vị trí cho IP {Ip}", lookupIp);
            return Failure(clientIp, lookupIp, usedServerPublicIp, "Không kết nối được tới dịch vụ tra cứu vị trí.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Dữ liệu trả về không hợp lệ khi tra cứu vị trí cho IP {Ip}", lookupIp);
            return Failure(clientIp, lookupIp, usedServerPublicIp, "Dữ liệu trả về từ dịch vụ tra cứu vị trí không hợp lệ.");
        }
    }

    private async Task<string?> GetServerPublicIpAsync(CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _httpClient
                .GetFromJsonAsync<PublicIpResponse>(_options.PublicIpLookupUrl, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            return IpAddressHelper.Normalize(payload?.Ip);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Không lấy được địa chỉ IP công khai của máy chủ.");
            return null;
        }
    }

    /// <summary>Chuỗi rỗng hoặc chỉ có khoảng trắng được coi là không có dữ liệu.</summary>
    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static GeoLocationInfo Failure(
        string clientIp,
        string lookupIp,
        bool usedServerPublicIp,
        string message) => new()
        {
            Success = false,
            ClientIp = clientIp,
            LookupIp = lookupIp,
            ResolvedUsingServerPublicIp = usedServerPublicIp,
            Provider = ProviderName,
            Error = message
        };

    // ---- DTO ánh xạ JSON của ipwho.is (https://ipwho.is) ----

    private sealed record IpWhoIsResponse
    {
        [JsonPropertyName("ip")] public string? Ip { get; init; }

        [JsonPropertyName("success")] public bool Success { get; init; }

        [JsonPropertyName("message")] public string? Message { get; init; }

        [JsonPropertyName("type")] public string? Type { get; init; }

        [JsonPropertyName("continent")] public string? Continent { get; init; }

        [JsonPropertyName("country")] public string? Country { get; init; }

        [JsonPropertyName("country_code")] public string? CountryCode { get; init; }

        [JsonPropertyName("region")] public string? Region { get; init; }

        [JsonPropertyName("region_code")] public string? RegionCode { get; init; }

        [JsonPropertyName("city")] public string? City { get; init; }

        [JsonPropertyName("postal")] public string? Postal { get; init; }

        [JsonPropertyName("latitude")] public double? Latitude { get; init; }

        [JsonPropertyName("longitude")] public double? Longitude { get; init; }

        [JsonPropertyName("connection")] public IpWhoIsConnection? Connection { get; init; }

        [JsonPropertyName("timezone")] public IpWhoIsTimezone? Timezone { get; init; }
    }

    private sealed record IpWhoIsConnection
    {
        [JsonPropertyName("asn")] public long? Asn { get; init; }

        [JsonPropertyName("org")] public string? Org { get; init; }

        [JsonPropertyName("isp")] public string? Isp { get; init; }

        [JsonPropertyName("domain")] public string? Domain { get; init; }
    }

    private sealed record IpWhoIsTimezone
    {
        [JsonPropertyName("id")] public string? Id { get; init; }

        [JsonPropertyName("utc")] public string? Utc { get; init; }
    }

    private sealed record PublicIpResponse
    {
        [JsonPropertyName("ip")] public string? Ip { get; init; }
    }
}
