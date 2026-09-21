# 40. REST API Controller + OpenAPI 定义

> 本章节提供所有 API 端点的 Controller 骨架、路由定义、请求/响应模型和 OpenAPI 注解。

## 40.1 统一响应模型

```csharp
namespace CryptoPlatform.Api.Models;

public sealed class ApiResponse<T>
{
    public int Code { get; init; } = 0;
    public string Message { get; init; } = "success";
    public T? Data { get; init; }
    public string RequestId { get; init; } = "";
    public string Timestamp { get; init; } = "";

    public static ApiResponse<T> Ok(T data, string requestId) => new()
    {
        Data = data, RequestId = requestId,
        Timestamp = DateTime.UtcNow.ToString("O")
    };

    public static ApiResponse<T> Fail(int code, string message, string requestId) => new()
    {
        Code = code, Message = message, RequestId = requestId,
        Timestamp = DateTime.UtcNow.ToString("O")
    };
}

public sealed class ApiErrorResponse
{
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
    public string RequestId { get; init; } = "";
    public string Timestamp { get; init; } = "";
}
```

## 40.2 管理认证 Controller

```csharp
namespace CryptoPlatform.Api.Controllers;

[ApiController]
[Route("api/v1/admin/auth")]
[Tags("管理认证")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly IAdminTokenService _tokenService;

    public AdminAuthController(IAdminTokenService tokenService) => _tokenService = tokenService;

    [HttpPost("token")]
    [AllowAnonymous]
    [OperationSummary("管理员登录获取 Token")]
    public async Task<ActionResult<ApiResponse<AdminLoginResponse>>> Login(
        [FromBody] AdminLoginRequest request, CancellationToken ct)
    {
        var result = await _tokenService.LoginAsync(request.Username, request.Password, ct);
        return Ok(ApiResponse<AdminLoginResponse>.Ok(result, Request.TraceIdentifier));
    }

    [HttpPost("token/revoke")]
    [OperationSummary("撤销 Token")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeToken(CancellationToken ct)
    {
        var jti = User.FindFirst("jti")?.Value;
        if (jti is not null)
            await _tokenService.RevokeAsync(jti, ct);
        return Ok(ApiResponse<object>.Ok(null!, Request.TraceIdentifier));
    }
}

public sealed record AdminLoginRequest(string Username, string Password);
```

## 40.3 密钥管理 Controller

```csharp
namespace CryptoPlatform.Api.Controllers;

[ApiController]
[Route("api/v1/admin/keys")]
[Tags("密钥管理")]
public sealed class KeyManagementController : ControllerBase
{
    private readonly IKeyService _keyService;

    public KeyManagementController(IKeyService keyService) => _keyService = keyService;

    [HttpPost]
    [OperationSummary("创建密钥")]
    public async Task<ActionResult<ApiResponse<KeyDescriptor>>> Create(
        [FromBody] CreateKeyCommand cmd, CancellationToken ct)
    {
        var result = await _keyService.CreateAsync(cmd, ct);
        return Ok(ApiResponse<KeyDescriptor>.Ok(result, Request.TraceIdentifier));
    }

    [HttpGet("{keyId}")]
    [OperationSummary("查询密钥元数据")]
    public async Task<ActionResult<ApiResponse<KeyDescriptor>>> Get(
        [FromRoute] string keyId, CancellationToken ct)
    {
        var result = await _keyService.GetAsync(keyId, ct);
        return Ok(ApiResponse<KeyDescriptor>.Ok(result, Request.TraceIdentifier));
    }

    [HttpPost("{keyId}/activate")]
    [OperationSummary("激活密钥")]
    public async Task<ActionResult<ApiResponse<KeyDescriptor>>> Activate(
        [FromRoute] string keyId, CancellationToken ct)
    {
        var result = await _keyService.ActivateAsync(keyId, ct);
        return Ok(ApiResponse<KeyDescriptor>.Ok(result, Request.TraceIdentifier));
    }

    [HttpPost("{keyId}/rotate")]
    [OperationSummary("轮换密钥")]
    public async Task<ActionResult<ApiResponse<KeyDescriptor>>> Rotate(
        [FromRoute] string keyId, [FromBody] RotateKeyCommand cmd, CancellationToken ct)
    {
        var result = await _keyService.RotateAsync(keyId, cmd, ct);
        return Ok(ApiResponse<KeyDescriptor>.Ok(result, Request.TraceIdentifier));
    }

    [HttpPost("{keyId}/disable")]
    [OperationSummary("停用密钥")]
    public async Task<ActionResult<ApiResponse<object>>> Disable(
        [FromRoute] string keyId, CancellationToken ct)
    {
        await _keyService.DisableAsync(keyId, ct);
        return Ok(ApiResponse<object>.Ok(null!, Request.TraceIdentifier));
    }

    [HttpPost("{keyId}/revoke")]
    [OperationSummary("撤销密钥")]
    public async Task<ActionResult<ApiResponse<object>>> Revoke(
        [FromRoute] string keyId, CancellationToken ct)
    {
        await _keyService.RevokeAsync(keyId, ct);
        return Ok(ApiResponse<object>.Ok(null!, Request.TraceIdentifier));
    }

    [HttpPost("{keyId}/destroy")]
    [OperationSummary("销毁密钥")]
    public async Task<ActionResult<ApiResponse<object>>> Destroy(
        [FromRoute] string keyId, [FromBody] DestroyKeyCommand cmd, CancellationToken ct)
    {
        await _keyService.DestroyAsync(keyId, cmd, ct);
        return Ok(ApiResponse<object>.Ok(null!, Request.TraceIdentifier));
    }

    [HttpGet("{keyId}/versions")]
    [OperationSummary("查询密钥版本列表")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KeyVersionDescriptor>>>> GetVersions(
        [FromRoute] string keyId, CancellationToken ct)
    {
        var result = await _keyService.GetVersionsAsync(keyId, ct);
        return Ok(ApiResponse<IReadOnlyList<KeyVersionDescriptor>>.Ok(result, Request.TraceIdentifier));
    }

    [HttpPost("import")]
    [OperationSummary("导入密钥")]
    public async Task<ActionResult<ApiResponse<KeyDescriptor>>> Import(
        [FromBody] ImportKeyCommand cmd, CancellationToken ct)
    {
        var result = await _keyService.ImportAsync(cmd, ct);
        return Ok(ApiResponse<KeyDescriptor>.Ok(result, Request.TraceIdentifier));
    }
}
```

## 40.4 密码业务 Controller（对外）

```csharp
namespace CryptoPlatform.Api.Controllers;

[ApiController]
[Route("api/v1/crypto")]
[Tags("密码业务")]
public sealed class CryptoController : ControllerBase
{
    private readonly ICryptoService _crypto;

    public CryptoController(ICryptoService crypto) => _crypto = crypto;

    // ─── SM4 ───

    [HttpPost("sm4/encrypt")]
    [OperationSummary("SM4 加密")]
    public async Task<ActionResult<ApiResponse<Sm4EncryptResponse>>> Sm4Encrypt(
        [FromBody] Sm4EncryptRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm4EncryptAsync(req, ct);
        return Ok(ApiResponse<Sm4EncryptResponse>.Ok(result, Request.TraceIdentifier));
    }

    [HttpPost("sm4/decrypt")]
    [OperationSummary("SM4 解密")]
    public async Task<ActionResult<ApiResponse<Sm4DecryptResponse>>> Sm4Decrypt(
        [FromBody] Sm4DecryptRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm4DecryptAsync(req, ct);
        return Ok(ApiResponse<Sm4DecryptResponse>.Ok(result, Request.TraceIdentifier));
    }

    // ─── SM2 ───

    [HttpPost("sm2/encrypt")]
    [OperationSummary("SM2 加密")]
    public async Task<ActionResult<ApiResponse<Sm2EncryptResponse>>> Sm2Encrypt(
        [FromBody] Sm2EncryptRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm2EncryptAsync(req, ct);
        return Ok(ApiResponse<Sm2EncryptResponse>.Ok(result, Request.TraceIdentifier));
    }

    [HttpPost("sm2/decrypt")]
    [OperationSummary("SM2 解密")]
    public async Task<ActionResult<ApiResponse<Sm2DecryptResponse>>> Sm2Decrypt(
        [FromBody] Sm2DecryptRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm2DecryptAsync(req, ct);
        return Ok(ApiResponse<Sm2DecryptResponse>.Ok(result, Request.TraceIdentifier));
    }

    [HttpPost("sm2/sign")]
    [OperationSummary("SM2 签名")]
    public async Task<ActionResult<ApiResponse<Sm2SignResponse>>> Sm2Sign(
        [FromBody] Sm2SignRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm2SignAsync(req, ct);
        return Ok(ApiResponse<Sm2SignResponse>.Ok(result, Request.TraceIdentifier));
    }

    [HttpPost("sm2/verify")]
    [OperationSummary("SM2 验签")]
    public async Task<ActionResult<ApiResponse<Sm2VerifyResponse>>> Sm2Verify(
        [FromBody] Sm2VerifyRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm2VerifyAsync(req, ct);
        return Ok(ApiResponse<Sm2VerifyResponse>.Ok(result, Request.TraceIdentifier));
    }

    // ─── SM3 ───

    [HttpPost("sm3/hash")]
    [OperationSummary("SM3 哈希")]
    public async Task<ActionResult<ApiResponse<Sm3HashResponse>>> Sm3Hash(
        [FromBody] Sm3HashRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm3HashAsync(req, ct);
        return Ok(ApiResponse<Sm3HashResponse>.Ok(result, Request.TraceIdentifier));
    }

    // ─── HMAC ───

    [HttpPost("hmac/generate")]
    [OperationSummary("HMAC-SM3 生成")]
    public async Task<ActionResult<ApiResponse<HmacGenerateResponse>>> HmacGenerate(
        [FromBody] HmacGenerateRequest req, CancellationToken ct)
    {
        var result = await _crypto.HmacGenerateAsync(req, ct);
        return Ok(ApiResponse<HmacGenerateResponse>.Ok(result, Request.TraceIdentifier));
    }

    [HttpPost("hmac/verify")]
    [OperationSummary("HMAC-SM3 校验")]
    public async Task<ActionResult<ApiResponse<HmacVerifyResponse>>> HmacVerify(
        [FromBody] HmacVerifyRequest req, CancellationToken ct)
    {
        var result = await _crypto.HmacVerifyAsync(req, ct);
        return Ok(ApiResponse<HmacVerifyResponse>.Ok(result, Request.TraceIdentifier));
    }

    // ─── Random ───

    [HttpPost("random")]
    [OperationSummary("生成随机数")]
    public async Task<ActionResult<ApiResponse<RandomResponse>>> Random(
        [FromBody] RandomRequest req, CancellationToken ct)
    {
        var result = await _crypto.GenerateRandomAsync(req, ct);
        return Ok(ApiResponse<RandomResponse>.Ok(result, Request.TraceIdentifier));
    }
}
```

## 40.5 全局异常处理中间件

```csharp
namespace CryptoPlatform.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "业务异常: {Code} - {Message}", ex.Code, ex.Message);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Code = ex.Code, Message = ex.Message,
                RequestId = context.TraceIdentifier,
                Timestamp = DateTime.UtcNow.ToString("O")
            });
        }
        catch (CryptoProviderException ex)
        {
            _logger.LogError(ex, "Provider 异常: {Code}", ex.ErrorCode);
            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Code = ex.ErrorCode, Message = "密码服务暂时不可用",
                RequestId = context.TraceIdentifier,
                Timestamp = DateTime.UtcNow.ToString("O")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理异常");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Code = "SYSTEM_ERROR", Message = "系统内部错误",
                RequestId = context.TraceIdentifier,
                Timestamp = DateTime.UtcNow.ToString("O")
            });
        }
    }
}
```

## 40.6 Program.cs 组装

```csharp
var builder = WebApplication.CreateBuilder(args);

// 基础设施
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddCryptoProviders();

// 业务服务
builder.Services.AddKeyService();
builder.Services.AddCryptoService();
builder.Services.AddAuditServices();

// Web
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "国密加密服务平台", Version = "v1" });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<AuthenticationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
```

## 40.7 API 端点汇总

| 方法 | 路径 | 认证方式 | 说明 |
|------|------|----------|------|
| POST | `/api/v1/admin/auth/token` | 无（公开） | 管理员登录 |
| POST | `/api/v1/admin/auth/token/revoke` | Token | 撤销 Token |
| POST | `/api/v1/admin/keys` | Token | 创建密钥 |
| GET | `/api/v1/admin/keys/{keyId}` | Token | 查询密钥 |
| POST | `/api/v1/admin/keys/{keyId}/activate` | Token | 激活密钥 |
| POST | `/api/v1/admin/keys/{keyId}/rotate` | Token | 轮换密钥 |
| POST | `/api/v1/admin/keys/{keyId}/disable` | Token | 停用密钥 |
| POST | `/api/v1/admin/keys/{keyId}/revoke` | Token | 撤销密钥 |
| POST | `/api/v1/admin/keys/{keyId}/destroy` | Token | 销毁密钥 |
| GET | `/api/v1/admin/keys/{keyId}/versions` | Token | 版本列表 |
| POST | `/api/v1/admin/keys/import` | Token | 导入密钥 |
| POST | `/api/v1/crypto/sm4/encrypt` | AppSecret | SM4 加密 |
| POST | `/api/v1/crypto/sm4/decrypt` | AppSecret | SM4 解密 |
| POST | `/api/v1/crypto/sm2/encrypt` | AppSecret | SM2 加密 |
| POST | `/api/v1/crypto/sm2/decrypt` | AppSecret | SM2 解密 |
| POST | `/api/v1/crypto/sm2/sign` | AppSecret | SM2 签名 |
| POST | `/api/v1/crypto/sm2/verify` | AppSecret | SM2 验签 |
| POST | `/api/v1/crypto/sm3/hash` | AppSecret | SM3 哈希 |
| POST | `/api/v1/crypto/hmac/generate` | AppSecret | HMAC 生成 |
| POST | `/api/v1/crypto/hmac/verify` | AppSecret | HMAC 校验 |
| POST | `/api/v1/crypto/random` | AppSecret | 随机数 |
