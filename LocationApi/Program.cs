using LocationApi.Endpoints;
using LocationApi.Options;
using LocationApi.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "LocationApiCors";

// Một số nền tảng PaaS (Render, Railway, Heroku, Google Cloud Run...) truyền cổng
// cần lắng nghe qua biến môi trường PORT thay vì ASPNETCORE_URLS.
var portFromEnvironment = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(portFromEnvironment))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{portFromEnvironment}");
}

// ---------- Cấu hình ----------
builder.Services.Configure<GeoLocationOptions>(
    builder.Configuration.GetSection(GeoLocationOptions.SectionName));

// ---------- Đăng ký dịch vụ ----------
builder.Services.AddMemoryCache();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Xác định IP thật của client (hỗ trợ chạy sau proxy/CDN).
builder.Services.AddSingleton<IClientIpResolver, ClientIpResolver>();

// Nhận diện thiết bị/trình duyệt từ User-Agent.
builder.Services.AddSingleton<IDeviceInfoService, UaParserDeviceInfoService>();

// Tra cứu vị trí theo IP; HttpClient được quản lý vòng đời tự động.
builder.Services.AddHttpClient<IGeoLocationService, IpWhoIsGeoLocationService>((serviceProvider, httpClient) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<GeoLocationOptions>>().Value;

    httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 60));
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LocationApi/1.0");
});

// Cho phép mọi nguồn gọi API (đổi lại theo nhu cầu khi triển khai thật).
builder.Services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// ---------- Pipeline xử lý request ----------
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors(CorsPolicyName);

// Phục vụ trang demo tĩnh trong wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapClientInfoEndpoints();

app.Run();
