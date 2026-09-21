using System.Collections.Concurrent;
using System.Text.Json;
using CryptoPlatform.Api.Models;

namespace CryptoPlatform.Api.Middleware;

/// <summary>
/// 幂等控制中间件。
/// 对指定路径的 POST 请求，通过 Idempotency-Key 请求头确保同一操作只执行一次。
/// 开发阶段使用内存存储，生产环境应替换为 Redis。
/// </summary>
public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, IdempotencyRecord> _store = new();

    /// <summary>需要幂等控制的请求路径前缀</summary>
    private static readonly HashSet<string> IdempotentPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/admin/keys",
        "/api/v1/admin/keys/import",
        "/api/v1/crypto/sm4/encrypt",
        "/api/v1/crypto/sm4/decrypt",
        "/api/v1/crypto/sm2/encrypt",
        "/api/v1/crypto/sm2/decrypt",
        "/api/v1/crypto/sm2/sign",
        "/api/v1/crypto/sm2/verify",
        "/api/v1/crypto/sm3/hash",
        "/api/v1/crypto/hmac/generate",
        "/api/v1/crypto/hmac/verify",
    };

    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private const int RecordTtlMinutes = 30;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // 仅对匹配的 POST 请求启用幂等控制
        if (context.Request.Method != "POST" || !IsIdempotentPath(path))
        {
            await _next(context);
            return;
        }

        // 检查是否提供了 Idempotency-Key
        if (!context.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var idempotencyKey)
            || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var key = $"{idempotencyKey}:{path}";

        // 检查是否已有缓存的响应
        if (_store.TryGetValue(key, out var existing) && !existing.IsExpired())
        {
            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.ResponseBody!);
            return;
        }

        // 执行请求并缓存响应
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await _next(context);

        memoryStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
        memoryStream.Seek(0, SeekOrigin.Begin);
        await memoryStream.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;

        // 仅缓存成功的响应（2xx）
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            _store[key] = new IdempotencyRecord(
                context.Response.StatusCode,
                responseBody,
                DateTime.UtcNow.AddMinutes(RecordTtlMinutes));
        }

        // 清理过期记录
        CleanupExpired();
    }

    private static bool IsIdempotentPath(string path)
    {
        foreach (var prefix in IdempotentPaths)
        {
            if (path.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void CleanupExpired()
    {
        var expiredKeys = _store.Where(kv => kv.Value.IsExpired()).Select(kv => kv.Key).ToList();
        foreach (var key in expiredKeys)
            _store.TryRemove(key, out _);
    }

    private sealed record IdempotencyRecord(int StatusCode, string ResponseBody, DateTime ExpiresAt)
    {
        public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
    }
}
