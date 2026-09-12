using LocationApi.Models;
using LocationApi.Services;

namespace LocationApi.Endpoints;

/// <summary>Khai báo toàn bộ endpoint của API.</summary>
public static class ClientInfoEndpoints
{
    public static IEndpointRouteBuilder MapClientInfoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Client Info");

        group.MapGet("/ip", GetIp)
            .WithName("GetClientIp")
            .WithSummary("Lấy địa chỉ IP của client đang truy cập")
            .WithDescription("Đọc IP từ header do proxy/CDN gửi tới (CF-Connecting-IP, True-Client-IP, X-Real-IP, X-Forwarded-For) rồi mới tới địa chỉ kết nối thực tế.")
            .Produces<ClientIpResponse>();

        group.MapGet("/location", GetLocationAsync)
            .WithName("GetLocationByIp")
            .WithSummary("Lấy vị trí hiện tại ước lượng theo địa chỉ IP")
            .WithDescription("Tra cứu quốc gia, thành phố, toạ độ, múi giờ, ISP của IP client qua dịch vụ ipwho.is. Kết quả được cache trong bộ nhớ.")
            .Produces<GeoLocationInfo>();

        group.MapGet("/device", GetDevice)
            .WithName("GetDeviceInfo")
            .WithSummary("Lấy thông tin thiết bị, hệ điều hành và trình duyệt")
            .WithDescription("Phân tích User-Agent bằng UAParser, kết hợp User-Agent Client Hints (Sec-CH-UA-*) khi trình duyệt hỗ trợ.")
            .Produces<DeviceInfo>();

        group.MapGet("/client-info", GetClientInfoAsync)
            .WithName("GetClientInfo")
            .WithSummary("Lấy tổng hợp IP + vị trí + thiết bị trong một lần gọi")
            .Produces<ClientInfoResponse>();

        group.MapPost("/location/precise", PostPreciseLocationAsync)
            .WithName("PostPreciseLocation")
            .WithSummary("Đối chiếu toạ độ GPS chính xác từ trình duyệt với vị trí ước lượng theo IP")
            .Produces<PreciseLocationResponse>();

        return app;
    }

    private static IResult GetIp(HttpContext context, IClientIpResolver ipResolver)
    {
        var ip = ipResolver.Resolve(context);
        var isPublic = ipResolver.IsPublic(ip);

        return Results.Ok(new ClientIpResponse
        {
            Ip = ip,
            IsPublic = isPublic,
            Note = isPublic
                ? null
                : "Đây là địa chỉ IP nội bộ/loopback (thường gặp khi chạy trên máy cục bộ), không thể tra cứu vị trí trên Internet."
        });
    }

    private static async Task<IResult> GetLocationAsync(
        HttpContext context,
        IClientIpResolver ipResolver,
        IGeoLocationService geoLocationService,
        CancellationToken cancellationToken)
    {
        var ip = ipResolver.Resolve(context);
        var location = await geoLocationService.GetAsync(ip, cancellationToken);

        return Results.Ok(location);
    }

    private static IResult GetDevice(HttpContext context, IDeviceInfoService deviceInfoService)
        => Results.Ok(deviceInfoService.Get(context));

    private static async Task<IResult> GetClientInfoAsync(
        HttpContext context,
        IClientIpResolver ipResolver,
        IGeoLocationService geoLocationService,
        IDeviceInfoService deviceInfoService,
        CancellationToken cancellationToken)
    {
        var ip = ipResolver.Resolve(context);

        // Gọi tra cứu vị trí và phân tích thiết bị song song cho nhanh.
        var locationTask = geoLocationService.GetAsync(ip, cancellationToken);
        var device = deviceInfoService.Get(context);
        var location = await locationTask;

        return Results.Ok(new ClientInfoResponse
        {
            Ip = ip,
            IsPublicIp = ipResolver.IsPublic(ip),
            RequestedAtUtc = DateTimeOffset.UtcNow,
            Location = location,
            Device = device,
            Request = RequestInfo.From(context)
        });
    }

    private static async Task<IResult> PostPreciseLocationAsync(
        PreciseLocationRequest request,
        HttpContext context,
        IClientIpResolver ipResolver,
        IGeoLocationService geoLocationService,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Latitude is < -90 or > 90)
        {
            errors["latitude"] = ["Vĩ độ phải nằm trong khoảng -90 đến 90."];
        }

        if (request.Longitude is < -180 or > 180)
        {
            errors["longitude"] = ["Kinh độ phải nằm trong khoảng -180 đến 180."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var browserLocation = new BrowserLocation
        {
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AccuracyMeters = request.AccuracyMeters,
            AltitudeMeters = request.AltitudeMeters,
            Source = string.IsNullOrWhiteSpace(request.Source) ? "browser-geolocation" : request.Source
        };

        var ip = ipResolver.Resolve(context);
        var ipLocation = await geoLocationService.GetAsync(ip, cancellationToken);

        double? distanceKm = null;

        if (ipLocation is { Success: true, Latitude: not null, Longitude: not null })
        {
            distanceKm = GeoDistance.Kilometers(
                browserLocation.Latitude,
                browserLocation.Longitude,
                ipLocation.Latitude.Value,
                ipLocation.Longitude.Value);
        }

        return Results.Ok(new PreciseLocationResponse
        {
            Success = true,
            BrowserLocation = browserLocation,
            IpLocation = ipLocation,
            DistanceFromIpLocationKm = distanceKm
        });
    }
}
