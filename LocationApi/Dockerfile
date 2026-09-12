# ============================================================================
#  Dockerfile đa tầng (multi-stage) cho LocationApi - ASP.NET Core .NET 10
#  Dùng được cho: Azure App Service, Render, Railway, Fly.io, Google Cloud Run,
#  VPS tự quản, ...
# ============================================================================

# ---------- Giai đoạn 1: build & publish ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy riêng file csproj trước để tận dụng cache của Docker khi restore.
COPY ["LocationApi.csproj", "./"]
RUN dotnet restore "LocationApi.csproj"

# Copy toàn bộ mã nguồn còn lại rồi publish bản Release.
COPY . .
RUN dotnet publish "LocationApi.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------- Giai đoạn 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Chạy dưới user không phải root cho an toàn.
USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LocationApi.dll"]
