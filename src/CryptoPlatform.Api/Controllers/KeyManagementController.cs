using CryptoPlatform.Api.Models;
using CryptoPlatform.Application.Keys;
using Microsoft.AspNetCore.Mvc;

namespace CryptoPlatform.Api.Controllers;

/// <summary>
/// 密钥管理控制器。提供密钥全生命周期管理端点。
/// </summary>
[ApiController]
[Route("api/v1/admin/keys")]
[Tags("密钥管理")]
public sealed class KeyManagementController : ControllerBase
{
    private readonly IKeyService _keyService;

    public KeyManagementController(IKeyService keyService) => _keyService = keyService;

    /// <summary>创建密钥</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<KeyDescriptor>>> Create(
        [FromBody] CreateKeyCommand cmd, CancellationToken ct)
    {
        var result = await _keyService.CreateAsync(cmd, ct);
        return Ok(ApiResponse<KeyDescriptor>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>查询密钥元数据</summary>
    [HttpGet("{keyId}")]
    public async Task<ActionResult<ApiResponse<KeyDescriptor>>> Get(
        [FromRoute] string keyId, CancellationToken ct)
    {
        var result = await _keyService.GetAsync(keyId, ct);
        return Ok(ApiResponse<KeyDescriptor>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>激活密钥</summary>
    [HttpPost("{keyId}/activate")]
    public async Task<ActionResult<ApiResponse<KeyDescriptor>>> Activate(
        [FromRoute] string keyId, CancellationToken ct)
    {
        var result = await _keyService.ActivateAsync(keyId, ct);
        return Ok(ApiResponse<KeyDescriptor>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>轮换密钥</summary>
    [HttpPost("{keyId}/rotate")]
    public async Task<ActionResult<ApiResponse<KeyDescriptor>>> Rotate(
        [FromRoute] string keyId, [FromBody] RotateKeyCommand cmd, CancellationToken ct)
    {
        var result = await _keyService.RotateAsync(keyId, cmd, ct);
        return Ok(ApiResponse<KeyDescriptor>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>停用密钥</summary>
    [HttpPost("{keyId}/disable")]
    public async Task<ActionResult<ApiResponse<object>>> Disable(
        [FromRoute] string keyId, CancellationToken ct)
    {
        await _keyService.DisableAsync(keyId, ct);
        return Ok(ApiResponse<object>.Ok(null!, HttpContext.TraceIdentifier));
    }

    /// <summary>撤销密钥</summary>
    [HttpPost("{keyId}/revoke")]
    public async Task<ActionResult<ApiResponse<object>>> Revoke(
        [FromRoute] string keyId, CancellationToken ct)
    {
        await _keyService.RevokeAsync(keyId, ct);
        return Ok(ApiResponse<object>.Ok(null!, HttpContext.TraceIdentifier));
    }

    /// <summary>销毁密钥</summary>
    [HttpPost("{keyId}/destroy")]
    public async Task<ActionResult<ApiResponse<object>>> Destroy(
        [FromRoute] string keyId, [FromBody] DestroyKeyCommand cmd, CancellationToken ct)
    {
        await _keyService.DestroyAsync(keyId, cmd, ct);
        return Ok(ApiResponse<object>.Ok(null!, HttpContext.TraceIdentifier));
    }

    /// <summary>查询密钥版本列表</summary>
    [HttpGet("{keyId}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KeyVersionDescriptor>>>> GetVersions(
        [FromRoute] string keyId, CancellationToken ct)
    {
        var result = await _keyService.GetVersionsAsync(keyId, ct);
        return Ok(ApiResponse<IReadOnlyList<KeyVersionDescriptor>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>导入密钥</summary>
    [HttpPost("import")]
    public async Task<ActionResult<ApiResponse<KeyDescriptor>>> Import(
        [FromBody] ImportKeyCommand cmd, CancellationToken ct)
    {
        var result = await _keyService.ImportAsync(cmd, ct);
        return Ok(ApiResponse<KeyDescriptor>.Ok(result, HttpContext.TraceIdentifier));
    }
}
