using System.ComponentModel.DataAnnotations;

namespace LocationApi.Models;

/// <summary>Payload mà trang web gửi lên khi người dùng đồng ý chia sẻ vị trí GPS.</summary>
public sealed record PreciseLocationRequest
{
    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }

    [Range(0, 10_000_000)]
    public double? AccuracyMeters { get; init; }

    public double? AltitudeMeters { get; init; }

    public string? Source { get; init; }
}
