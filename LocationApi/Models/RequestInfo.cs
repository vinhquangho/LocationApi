using Microsoft.Net.Http.Headers;

namespace LocationApi.Models;

/// <summary>Thông tin về chính yêu cầu HTTP mà client đã gửi tới API.</summary>
public sealed record RequestInfo
{
    public string? Method { get; init; }

    public string? Path { get; init; }

    public string? Scheme { get; init; }

    public string? Host { get; init; }

    public string? Protocol { get; init; }

    public bool IsHttps { get; init; }

    public string? Referer { get; init; }

    public string? Origin { get; init; }

    public string? TraceIdentifier { get; init; }

    /// <summary>Các IP nằm trong chuỗi X-Forwarded-For (nếu client đi qua proxy/CDN).</summary>
    public IReadOnlyList<string> ForwardedFor { get; init; } = [];

    public static RequestInfo From(HttpContext context)
    {
        var request = context.Request;

        var forwardedFor = new List<string>();
        if (request.Headers.TryGetValue("X-Forwarded-For", out var xff) &&
            !string.IsNullOrWhiteSpace(xff.ToString()))
        {
            forwardedFor.AddRange(xff
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return new RequestInfo
        {
            Method = request.Method,
            Path = request.Path.Value,
            Scheme = request.Scheme,
            Host = request.Host.Value,
            Protocol = request.Protocol,
            IsHttps = request.IsHttps,
            Referer = request.Headers[HeaderNames.Referer].ToString() is { Length: > 0 } referer ? referer : null,
            Origin = request.Headers[HeaderNames.Origin].ToString() is { Length: > 0 } origin ? origin : null,
            TraceIdentifier = context.TraceIdentifier,
            ForwardedFor = forwardedFor
        };
    }
}
