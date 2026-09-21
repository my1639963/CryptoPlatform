using Microsoft.AspNetCore.Http;

namespace CryptoPlatform.Application;

/// <summary>
/// 基于 HTTP 上下文的操作上下文实现。
/// 从 HttpContext 的 Items / Headers 中读取认证中间件设置的操作者信息。
/// </summary>
public sealed class HttpOperationContext : IOperationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpOperationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

    public string AppId
    {
        get
        {
            var ctx = HttpContext;
            if (ctx is null) return "SYSTEM";
            return ctx.Items.TryGetValue("AppId", out var appId) && appId is string s
                ? s
                : "SYSTEM";
        }
    }

    public string RequestId
    {
        get
        {
            var ctx = HttpContext;
            if (ctx is null) return Guid.NewGuid().ToString("N");
            return ctx.TraceIdentifier;
        }
    }

    public string OperatorType
    {
        get
        {
            var ctx = HttpContext;
            if (ctx is null) return "SYSTEM";
            return ctx.Items.TryGetValue("OperatorType", out var opType) && opType is string s
                ? s
                : "SYSTEM";
        }
    }

    public string? OperatorId
    {
        get
        {
            var ctx = HttpContext;
            if (ctx is null) return null;
            return ctx.Items.TryGetValue("OperatorId", out var opId) && opId is string s
                ? s
                : null;
        }
    }
}
