using CryptoPlatform.Api.Models;
using CryptoPlatform.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CryptoPlatform.Api.Controllers;

/// <summary>
/// 管理员认证控制器。提供登录和令牌撤销端点。
/// </summary>
[ApiController]
[Route("api/v1/admin/auth")]
[Tags("管理认证")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly IAdminTokenService _tokenService;

    public AdminAuthController(IAdminTokenService tokenService) => _tokenService = tokenService;

    /// <summary>管理员登录获取 Token</summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AdminLoginResponse>>> Login(
        [FromBody] AdminLoginRequest request, CancellationToken ct)
    {
        var result = await _tokenService.LoginAsync(request.Username, request.Password, ct);
        return Ok(ApiResponse<AdminLoginResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>撤销 Token</summary>
    [HttpPost("token/revoke")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeToken(CancellationToken ct)
    {
        var jti = User.FindFirst("jti")?.Value;
        if (jti is not null)
            await _tokenService.RevokeAsync(jti, ct);
        return Ok(ApiResponse<object>.Ok(null!, HttpContext.TraceIdentifier));
    }
}

/// <summary>管理员登录请求</summary>
public sealed record AdminLoginRequest(string Username, string Password);
