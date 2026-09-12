# LocationApi — Web API lấy vị trí hiện tại và thông tin thiết bị của client

Web API viết bằng **C# / ASP.NET Core (.NET 10)** dùng để:

1. Lấy **địa chỉ IP** của người đang truy cập (hỗ trợ chạy sau proxy/CDN).
2. Lấy **vị trí hiện tại** (quốc gia, tỉnh/thành, toạ độ, múi giờ, ISP) ước lượng từ địa chỉ IP đó.
3. Lấy **thông tin thiết bị**: loại thiết bị, hệ điều hành, trình duyệt, engine, hãng/model, kiến trúc CPU, ngôn ngữ…
4. (Tuỳ chọn) Nhận **toạ độ GPS chính xác** từ trình duyệt và đối chiếu với vị trí theo IP.

Kèm theo một **trang demo** đẹp mắt tại `http://localhost:5184/`.

---

## 1. Yêu cầu

- [.NET 10 SDK](https://dotnet.microsoft.com/download) trở lên.
- Kết nối Internet (để gọi dịch vụ tra cứu vị trí).

## 2. Chạy dự án

```bash
cd LocationApi
dotnet run
```

Sau đó mở:

| Địa chỉ | Mô tả |
| --- | --- |
| <http://localhost:5184/> | Trang demo trực quan |
| <http://localhost:5184/openapi/v1.json> | OpenAPI document (chỉ ở môi trường Development) |

## 3. Danh sách endpoint

| Method | Route | Mô tả |
| --- | --- | --- |
| `GET` | `/api/client-info` | **Tổng hợp** IP + vị trí + thiết bị + thông tin request |
| `GET` | `/api/ip` | Chỉ trả về địa chỉ IP của client |
| `GET` | `/api/location` | Vị trí ước lượng theo IP |
| `GET` | `/api/device` | Thông tin thiết bị / trình duyệt |
| `POST` | `/api/location/precise` | Gửi toạ độ GPS từ trình duyệt, so sánh với vị trí theo IP |

### Ví dụ phản hồi `GET /api/client-info`

```json
{
  "ip": "203.113.152.10",
  "isPublicIp": true,
  "requestedAtUtc": "2026-09-12T03:20:11.1234567+00:00",
  "location": {
    "success": true,
    "clientIp": "203.113.152.10",
    "lookupIp": "203.113.152.10",
    "resolvedUsingServerPublicIp": false,
    "ipType": "IPv4",
    "continent": "Asia",
    "country": "Vietnam",
    "countryCode": "VN",
    "region": "Hanoi",
    "city": "Hanoi",
    "latitude": 21.0278,
    "longitude": 105.8342,
    "timeZone": "Asia/Bangkok",
    "isp": "Viettel Group",
    "asn": "7552",
    "provider": "ipwho.is",
    "accuracy": "Theo IP: chính xác tới mức thành phố (~5-50 km)",
    "googleMapsUrl": "https://www.google.com/maps?q=21.0278,105.8342"
  },
  "device": {
    "deviceType": "Desktop",
    "operatingSystem": "Windows",
    "operatingSystemVersion": "10",
    "browser": "Chrome",
    "browserVersion": "124.0.6367",
    "browserEngine": "Blink",
    "isDesktop": true,
    "isMobile": false,
    "isBot": false
  },
  "request": {
    "method": "GET",
    "path": "/api/client-info",
    "isHttps": false,
    "forwardedFor": []
  }
}
```

### Ví dụ `POST /api/location/precise`

```json
{
  "latitude": 21.028511,
  "longitude": 105.804817,
  "accuracyMeters": 25,
  "source": "browser-geolocation"
}
```

Phản hồi trả về toạ độ GPS, vị trí theo IP và khoảng cách (km) giữa hai vị trí (công thức Haversine).

## 4. Cấu trúc mã nguồn

```
LocationApi/
├── Program.cs                          # Cấu hình DI + pipeline
├── appsettings.json                    # Cấu hình dịch vụ tra cứu vị trí
├── Endpoints/
│   └── ClientInfoEndpoints.cs          # Khai báo các endpoint (Minimal API)
├── Models/                             # DTO trả về / nhận vào
│   ├── ClientInfoResponse.cs
│   ├── ClientIpResponse.cs
│   ├── GeoLocationInfo.cs
│   ├── DeviceInfo.cs
│   ├── RequestInfo.cs
│   ├── BrowserLocation.cs
│   ├── PreciseLocationRequest.cs
│   └── PreciseLocationResponse.cs
├── Options/
│   └── GeoLocationOptions.cs           # Cấu hình section "GeoLocation"
├── Services/
│   ├── IpAddressHelper.cs              # Chuẩn hoá & phân loại IP
│   ├── IClientIpResolver.cs
│   ├── ClientIpResolver.cs             # Lấy IP thật khi chạy sau proxy/CDN
│   ├── IGeoLocationService.cs
│   ├── IpWhoIsGeoLocationService.cs    # Tra cứu vị trí qua ipwho.is + cache
│   ├── IDeviceInfoService.cs
│   ├── UaParserDeviceInfoService.cs    # Phân tích User-Agent + Client Hints
│   └── GeoDistance.cs                  # Công thức Haversine
└── wwwroot/index.html                  # Trang demo
```

## 5. Cấu hình (`appsettings.json`)

```json
"GeoLocation": {
  "BaseUrl": "https://ipwho.is",
  "PublicIpLookupUrl": "https://api.ipify.org?format=json",
  "TimeoutSeconds": 8,
  "CacheMinutes": 30
}
```

- Mặc định dùng **ipwho.is** — miễn phí, không cần API key, hỗ trợ HTTPS.
  Có thể thay bằng nhà cung cấp khác (ipinfo.io, ipapi.co…) bằng cách viết thêm một lớp implement `IGeoLocationService`.
- Kết quả tra cứu được **cache trong bộ nhớ** để tránh gọi dịch vụ ngoài quá nhiều lần.

## 6. Lưu ý quan trọng

### a) Độ chính xác của định vị theo IP

Định vị theo IP **không thể chính xác đến địa chỉ nhà**. Kết quả trả về chỉ tới mức
**thành phố / nhà cung cấp mạng**, sai số thường từ **5–50 km**, và có thể lệch hẳn
nếu nhà mạng dùng NAT hoặc IP của VPN/proxy. Nếu cần vị trí chính xác, hãy dùng
`navigator.geolocation` trong trình duyệt (endpoint `/api/location/precise`).

### b) IP khi chạy localhost

Khi chạy trên máy cục bộ, IP client là `::1` / `127.0.0.1` — không thể tra cứu. API sẽ tự
động lấy IP công khai của máy chủ (qua ipify) để tra cứu và đánh dấu
`resolvedUsingServerPublicIp: true`. Hãy kiểm thử với IP thật bằng header:

```http
GET /api/client-info
X-Forwarded-For: 203.113.152.10
```

### c) Chạy sau proxy/CDN

`ClientIpResolver` đọc IP theo thứ tự tin cậy:
`CF-Connecting-IP` → `True-Client-IP` → `X-Real-IP` → `X-Forwarded-For`.
Các header này **client có thể tự gửi**, nên khi triển khai thật bạn cần:

- Cấu hình reverse proxy (nginx, IIS ARR, Cloudflare) **ghi đè** các header đó.
- Hoặc siết lại logic chỉ tin tưởng header khi request đến từ IP của proxy.

### d) Thông tin thiết bị

Dữ liệu thiết bị lấy từ `User-Agent` (phân tích bằng thư viện **UAParser**) và
`User-Agent Client Hints` (`Sec-CH-UA`, `Sec-CH-UA-Platform`, `Sec-CH-UA-Mobile`).
Trình duyệt có thể cắt ngắn hoặc làm mờ User-Agent (ví dụ Chrome dùng
`User-Agent Reduction`), nên một số trường như phiên bản Windows có thể chỉ ở mức
đại khái. Thông tin như **độ phân giải màn hình, danh sách phần mềm, ID thiết bị**
không thể lấy được từ phía server — cần JavaScript ở client.

### e) Bảo mật

Đây là mã nguồn mẫu. Trước khi dùng thật, hãy cân nhắc:

- Giới hạn CORS theo domain cụ thể thay vì `AllowAnyOrigin`.
- Thêm rate limiting (`builder.Services.AddRateLimiter(...)`) để tránh bị lạm dụng.
- Bật HTTPS và ghi log việc thu thập dữ liệu vị trí cá nhân theo quy định hiện hành.

---

## 7. Deploy

### Render.com (miễn phí) — khuyến nghị

Repo đã có sẵn `render.yaml` (Blueprint) và `LocationApi/Dockerfile`.

1. Đăng ký/đăng nhập <https://render.com> bằng chính tài khoản GitHub.
2. Vào **Dashboard → New → Blueprint**.
3. Chọn repo `LocationApi` → **Connect** → **Apply**.
4. Render tự build Docker image và cấp cho bạn một URL dạng
   `https://location-api-xxxx.onrender.com`.

**Lưu ý về gói Free của Render:**

- Service sẽ **ngủ sau ~15 phút** không có truy cập; request đầu tiên sau đó mất
  khoảng 30–60 giây để "thức dậy" (cold start).
- Biến môi trường `PORT` do Render cung cấp đã được `Program.cs` đọc tự động.

### Chạy bằng Docker ở máy local

```bash
cd LocationApi
docker build -t location-api .
docker run -p 8080:8080 location-api
# Mở http://localhost:8080/
```

### Azure App Service

```bash
# Cần cài Azure CLI rồi đăng nhập: az login
az group create --name rg-locationapi --location southeastasia
az appservice plan create --name plan-locationapi --resource-group rg-locationapi --sku F1 --is-linux
az webapp create --name locationapi-unique-name --resource-group rg-locationapi \
  --plan plan-locationapi --runtime "DOTNETCORE:10.0"
az webapp deploy --resource-group rg-locationapi --name locationapi-unique-name \
  --src-path ./publish --type zip
```

### Fly.io / Railway / Google Cloud Run

Dùng đúng `LocationApi/Dockerfile` có sẵn — tất cả đều đọc biến `PORT`
hoặc `ASPNETCORE_HTTP_PORTS` nên chạy được không cần sửa code.

