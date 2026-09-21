using CryptoPlatform.Api.Models;

namespace CryptoPlatform.Api.Middleware;

/// <summary>
/// 请求 ID 中间件。确保每个请求都有唯一的 RequestId。
/// </summary>
public sealed class RequestIdMiddleware
{
    private const string RequestIdHeader = "X-Request-Id";
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(RequestIdHeader))
        {
            context.Request.Headers[RequestIdHeader] = Guid.NewGuid().ToString("N");
        }

        context.Response.Headers[RequestIdHeader] = context.TraceIdentifier;
        await _next(context);
    }
}
