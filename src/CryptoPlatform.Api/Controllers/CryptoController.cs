using CryptoPlatform.Api.Models;
using CryptoPlatform.Application.Crypto;
using Microsoft.AspNetCore.Mvc;

namespace CryptoPlatform.Api.Controllers;

/// <summary>
/// 密码业务控制器。提供 SM2/SM4/SM3/HMAC-SM3 全算法密码运算端点。
/// 对外 API 使用 AppSecret 签名认证。
/// </summary>
[ApiController]
[Route("api/v1/crypto")]
[Tags("密码业务")]
public sealed class CryptoController : ControllerBase
{
    private readonly ICryptoService _crypto;

    public CryptoController(ICryptoService crypto) => _crypto = crypto;

    // ─── SM4 ───

    /// <summary>SM4 加密</summary>
    [HttpPost("sm4/encrypt")]
    public async Task<ActionResult<ApiResponse<Sm4EncryptResponse>>> Sm4Encrypt(
        [FromBody] Sm4EncryptRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm4EncryptAsync(req, ct);
        return Ok(ApiResponse<Sm4EncryptResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>SM4 解密</summary>
    [HttpPost("sm4/decrypt")]
    public async Task<ActionResult<ApiResponse<Sm4DecryptResponse>>> Sm4Decrypt(
        [FromBody] Sm4DecryptRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm4DecryptAsync(req, ct);
        return Ok(ApiResponse<Sm4DecryptResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    // ─── SM2 ───

    /// <summary>SM2 加密</summary>
    [HttpPost("sm2/encrypt")]
    public async Task<ActionResult<ApiResponse<Sm2EncryptResponse>>> Sm2Encrypt(
        [FromBody] Sm2EncryptRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm2EncryptAsync(req, ct);
        return Ok(ApiResponse<Sm2EncryptResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>SM2 解密</summary>
    [HttpPost("sm2/decrypt")]
    public async Task<ActionResult<ApiResponse<Sm2DecryptResponse>>> Sm2Decrypt(
        [FromBody] Sm2DecryptRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm2DecryptAsync(req, ct);
        return Ok(ApiResponse<Sm2DecryptResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>SM2 签名</summary>
    [HttpPost("sm2/sign")]
    public async Task<ActionResult<ApiResponse<Sm2SignResponse>>> Sm2Sign(
        [FromBody] Sm2SignRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm2SignAsync(req, ct);
        return Ok(ApiResponse<Sm2SignResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>SM2 验签</summary>
    [HttpPost("sm2/verify")]
    public async Task<ActionResult<ApiResponse<Sm2VerifyResponse>>> Sm2Verify(
        [FromBody] Sm2VerifyRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm2VerifyAsync(req, ct);
        return Ok(ApiResponse<Sm2VerifyResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    // ─── SM3 ───

    /// <summary>SM3 哈希</summary>
    [HttpPost("sm3/hash")]
    public async Task<ActionResult<ApiResponse<Sm3HashResponse>>> Sm3Hash(
        [FromBody] Sm3HashRequest req, CancellationToken ct)
    {
        var result = await _crypto.Sm3HashAsync(req, ct);
        return Ok(ApiResponse<Sm3HashResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    // ─── HMAC-SM3 ───

    /// <summary>HMAC-SM3 生成</summary>
    [HttpPost("hmac/generate")]
    public async Task<ActionResult<ApiResponse<HmacGenerateResponse>>> HmacGenerate(
        [FromBody] HmacGenerateRequest req, CancellationToken ct)
    {
        var result = await _crypto.HmacGenerateAsync(req, ct);
        return Ok(ApiResponse<HmacGenerateResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>HMAC-SM3 校验</summary>
    [HttpPost("hmac/verify")]
    public async Task<ActionResult<ApiResponse<HmacVerifyResponse>>> HmacVerify(
        [FromBody] HmacVerifyRequest req, CancellationToken ct)
    {
        var result = await _crypto.HmacVerifyAsync(req, ct);
        return Ok(ApiResponse<HmacVerifyResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    // ─── Random ───

    /// <summary>生成随机数</summary>
    [HttpPost("random")]
    public async Task<ActionResult<ApiResponse<RandomResponse>>> Random(
        [FromBody] RandomRequest req, CancellationToken ct)
    {
        var result = await _crypto.GenerateRandomAsync(req, ct);
        return Ok(ApiResponse<RandomResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
