using CryptoPlatform.Api.Models;
using CryptoPlatform.Crypto.Abstractions;
using CryptoPlatform.Domain;

namespace CryptoPlatform.Api.Middleware;

/// <summary>
/// 全局异常处理中间件。将异常统一转换为 ApiResponse 格式。
/// 不暴露内部堆栈、SQL 或 HSM SDK 信息。
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "业务异常: {Code} - {Message}", ex.Code, ex.Message);
            context.Response.StatusCode = 200;
            await context.Response.WriteAsJsonAsync(ApiResponse.Fail(
                MapBusinessErrorCode(ex.Code), ex.Message, GetRequestId(context)));
        }
        catch (CryptoProviderException ex)
        {
            _logger.LogError(ex, "Provider 异常: {Code} - {Message}", ex.ErrorCode, ex.Message);
            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(ApiResponse.Fail(
                50301, "密码服务暂时不可用", GetRequestId(context)));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "资源未找到: {Message}", ex.Message);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(ApiResponse.Fail(
                404, ex.Message, GetRequestId(context)));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权访问: {Message}", ex.Message);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(ApiResponse.Fail(
                401, "未授权", GetRequestId(context)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理异常: {Message}", ex.Message);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(ApiResponse.Fail(
                500, "服务器内部错误", GetRequestId(context)));
        }
    }

    private static string GetRequestId(HttpContext context)
        => context.TraceIdentifier;

    private static int MapBusinessErrorCode(string code) => code switch
    {
        "KEY_NOT_FOUND" => 40401,
        "KEY_STATUS_INVALID" => 40001,
        "KEY_REVOKE_NOT_ALLOWED" => 40002,
        "KEY_DESTROY_NOT_ALLOWED" => 40003,
        "KEY_USAGE_NOT_ALLOWED" => 40004,
        "APP_NOT_FOUND" => 40402,
        "APP_DISABLED" => 40005,
        "APP_LOCKED" => 40006,
        "AUTH_FAILED" => 40101,
        "TOKEN_EXPIRED" => 40102,
        "TOKEN_REVOKED" => 40103,
        "SIGNATURE_INVALID" => 40104,
        "IDEMPOTENCY_KEY_REUSED" => 40007,
        "RATE_LIMIT_EXCEEDED" => 42901,
        _ => 50000
    };
}
