# 35. CryptoService 完整代码骨架

> 本章节提供密码运算服务的完整接口定义、各算法实现骨架和 Provider 调用流程。

## 35.1 请求/响应 DTO

```csharp
namespace CryptoPlatform.Application.Crypto;

// ─── SM4 ───
public sealed record Sm4EncryptRequest(
    string KeyId, int? KeyVersion, string Plaintext,
    string Mode, string Encoding, string? Nonce, string? Aad);

public sealed record Sm4EncryptResponse(
    string KeyId, int KeyVersion, string Algorithm,
    string Ciphertext, string Nonce, string Tag, string Encoding);

public sealed record Sm4DecryptRequest(
    string KeyId, int? KeyVersion, string Ciphertext,
    string Mode, string Encoding, string Nonce, string Tag, string? Aad);

public sealed record Sm4DecryptResponse(
    string KeyId, int KeyVersion, string Plaintext, string Encoding);

// ─── SM2 ───
public sealed record Sm2EncryptRequest(string KeyId, int? KeyVersion, string Data, string Encoding);
public sealed record Sm2EncryptResponse(string KeyId, int KeyVersion, string Ciphertext, string Encoding);
public sealed record Sm2DecryptRequest(string KeyId, int? KeyVersion, string Ciphertext, string Encoding);
public sealed record Sm2DecryptResponse(string KeyId, int KeyVersion, string Data, string Encoding);
public sealed record Sm2SignRequest(string KeyId, int? KeyVersion, string Data, string Encoding);
public sealed record Sm2SignResponse(string KeyId, int KeyVersion, string Signature, string Encoding);
public sealed record Sm2VerifyRequest(string KeyId, int? KeyVersion, string Data, string Signature, string Encoding);
public sealed record Sm2VerifyResponse(string KeyId, int KeyVersion, bool Valid);

// ─── SM3 ───
public sealed record Sm3HashRequest(string Data, string InputEncoding, string OutputEncoding);
public sealed record Sm3HashResponse(string Hash, string Encoding);
public sealed record Sm3HashStreamRequest(string KeyId, string InputEncoding, string OutputEncoding, long ChunkSize = 8192);

// ─── HMAC-SM3 ───
public sealed record HmacGenerateRequest(string KeyId, int? KeyVersion, string Data, string Encoding);
public sealed record HmacGenerateResponse(string KeyId, int KeyVersion, string Hmac, string Encoding);
public sealed record HmacVerifyRequest(string KeyId, int? KeyVersion, string Data, string Hmac, string Encoding);
public sealed record HmacVerifyResponse(string KeyId, int KeyVersion, bool Valid);

// ─── Random ───
public sealed record RandomRequest(int Length, string Encoding);
public sealed record RandomResponse(string Data, string Encoding);
```

## 35.2 ICryptoService 接口

```csharp
namespace CryptoPlatform.Application.Crypto;

public interface ICryptoService
{
    // SM4
    Task<Sm4EncryptResponse> Sm4EncryptAsync(Sm4EncryptRequest req, CancellationToken ct);
    Task<Sm4DecryptResponse> Sm4DecryptAsync(Sm4DecryptRequest req, CancellationToken ct);

    // SM2
    Task<Sm2EncryptResponse> Sm2EncryptAsync(Sm2EncryptRequest req, CancellationToken ct);
    Task<Sm2DecryptResponse> Sm2DecryptAsync(Sm2DecryptRequest req, CancellationToken ct);
    Task<Sm2SignResponse> Sm2SignAsync(Sm2SignRequest req, CancellationToken ct);
    Task<Sm2VerifyResponse> Sm2VerifyAsync(Sm2VerifyRequest req, CancellationToken ct);

    // SM3
    Task<Sm3HashResponse> Sm3HashAsync(Sm3HashRequest req, CancellationToken ct);
    Task<Sm3HashResponse> Sm3HashStreamAsync(Sm3HashStreamRequest req, CancellationToken ct);

    // HMAC-SM3
    Task<HmacGenerateResponse> HmacGenerateAsync(HmacGenerateRequest req, CancellationToken ct);
    Task<HmacVerifyResponse> HmacVerifyAsync(HmacVerifyRequest req, CancellationToken ct);

    // Random
    Task<RandomResponse> GenerateRandomAsync(RandomRequest req, CancellationToken ct);
}
```

## 35.3 CryptoService 实现

```csharp
namespace CryptoPlatform.Application.Crypto;

public sealed class CryptoService : ICryptoService
{
    private readonly CryptoPlatformDbContext _db;
    private readonly ICryptoProviderRouter _router;
    private readonly IKeyAuthorizationChecker _authChecker;
    private readonly IAuditService _audit;
    private readonly ILogger<CryptoService> _logger;

    public CryptoService(
        CryptoPlatformDbContext db,
        ICryptoProviderRouter router,
        IKeyAuthorizationChecker authChecker,
        IAuditService audit,
        ILogger<CryptoService> logger)
    {
        _db = db; _router = router; _authChecker = authChecker;
        _audit = audit; _logger = logger;
    }

    // ────────────────── SM4 Encrypt ──────────────────

    public async Task<Sm4EncryptResponse> Sm4EncryptAsync(Sm4EncryptRequest req, CancellationToken ct)
    {
        var (key, version) = await ResolveKeyAsync(req.KeyId, req.KeyVersion, "ENCRYPT", ct);
        var provider = _router.ResolveProvider(version);

        var plaintextBytes = Decode(req.Plaintext, req.Encoding);
        var parameters = new CryptoParameters
        {
            Mode = req.Mode,           // GCM / CBC / CTR / ECB
            Nonce = req.Nonce is not null ? Decode(req.Nonce, "BASE64") : null,
            Aad = req.Aad is not null ? Decode(req.Aad, "BASE64") : null
        };

        var result = await provider.EncryptAsync(version.ProviderKeyRef, plaintextBytes, parameters, ct);

        // Nonce 由 Provider 生成时回传
        var nonceOut = result.Nonce is not null ? Encode(result.Nonce, "BASE64") : req.Nonce!;

        await _audit.LogAsync("SM4_ENCRYPT", req.KeyId, version.VersionNo, "SUCCESS", ct);

        return new Sm4EncryptResponse(
            req.KeyId, version.VersionNo, $"SM4-{req.Mode}",
            Encode(result.Ciphertext, "BASE64"),
            nonceOut,
            Encode(result.Tag ?? Array.Empty<byte>(), "BASE64"),
            "BASE64");
    }

    // ────────────────── SM4 Decrypt ──────────────────

    public async Task<Sm4DecryptResponse> Sm4DecryptAsync(Sm4DecryptRequest req, CancellationToken ct)
    {
        var (key, version) = await ResolveKeyAsync(req.KeyId, req.KeyVersion, "DECRYPT", ct);
        var provider = _router.ResolveProvider(version);

        var parameters = new CryptoParameters
        {
            Mode = req.Mode,
            Nonce = Decode(req.Nonce, "BASE64"),
            Tag = Decode(req.Tag, "BASE64"),
            Aad = req.Aad is not null ? Decode(req.Aad, "BASE64") : null
        };

        var ciphertextBytes = Decode(req.Ciphertext, req.Encoding);
        var result = await provider.DecryptAsync(version.ProviderKeyRef, ciphertextBytes, parameters, ct);

        await _audit.LogAsync("SM4_DECRYPT", req.KeyId, version.VersionNo, "SUCCESS", ct);

        return new Sm4DecryptResponse(
            req.KeyId, version.VersionNo, Encode(result.Plaintext, "BASE64"), "BASE64");
    }

    // ────────────────── SM2 ──────────────────

    public async Task<Sm2EncryptResponse> Sm2EncryptAsync(Sm2EncryptRequest req, CancellationToken ct)
    {
        var (key, version) = await ResolveKeyAsync(req.KeyId, req.KeyVersion, "ENCRYPT", ct);
        var provider = _router.ResolveProvider(version);
        var data = Decode(req.Data, req.Encoding);
        var result = await provider.EncryptAsync(version.ProviderKeyRef, data, new CryptoParameters(), ct);
        await _audit.LogAsync("SM2_ENCRYPT", req.KeyId, version.VersionNo, "SUCCESS", ct);
        return new Sm2EncryptResponse(req.KeyId, version.VersionNo, Encode(result.Ciphertext, "BASE64"), "BASE64");
    }

    public async Task<Sm2DecryptResponse> Sm2DecryptAsync(Sm2DecryptRequest req, CancellationToken ct)
    {
        var (key, version) = await ResolveKeyAsync(req.KeyId, req.KeyVersion, "DECRYPT", ct);
        var provider = _router.ResolveProvider(version);
        var data = Decode(req.Ciphertext, req.Encoding);
        var result = await provider.DecryptAsync(version.ProviderKeyRef, data, new CryptoParameters(), ct);
        await _audit.LogAsync("SM2_DECRYPT", req.KeyId, version.VersionNo, "SUCCESS", ct);
        return new Sm2DecryptResponse(req.KeyId, version.VersionNo, Encode(result.Plaintext, "BASE64"), "BASE64");
    }

    public async Task<Sm2SignResponse> Sm2SignAsync(Sm2SignRequest req, CancellationToken ct)
    {
        var (key, version) = await ResolveKeyAsync(req.KeyId, req.KeyVersion, "SIGN", ct);
        var provider = _router.ResolveProvider(version);
        var data = Decode(req.Data, req.Encoding);
        var result = await provider.SignAsync(version.ProviderKeyRef, data, new CryptoParameters(), ct);
        await _audit.LogAsync("SM2_SIGN", req.KeyId, version.VersionNo, "SUCCESS", ct);
        return new Sm2SignResponse(req.KeyId, version.VersionNo, Encode(result.Signature, "BASE64"), "BASE64");
    }

    public async Task<Sm2VerifyResponse> Sm2VerifyAsync(Sm2VerifyRequest req, CancellationToken ct)
    {
        var (key, version) = await ResolveKeyAsync(req.KeyId, req.KeyVersion, "VERIFY", ct);
        var provider = _router.ResolveProvider(version);
        var data = Decode(req.Data, req.Encoding);
        var sig = Decode(req.Signature, req.Encoding);
        var valid = await provider.VerifyAsync(version.ProviderKeyRef, data, sig, new CryptoParameters(), ct);
        await _audit.LogAsync("SM2_VERIFY", req.KeyId, version.VersionNo, valid ? "SUCCESS" : "VERIFY_FAILED", ct);
        return new Sm2VerifyResponse(req.KeyId, version.VersionNo, valid);
    }

    // ────────────────── SM3 ──────────────────

    public async Task<Sm3HashResponse> Sm3HashAsync(Sm3HashRequest req, CancellationToken ct)
    {
        var provider = _router.GetDefaultProvider();
        var data = Decode(req.Data, req.InputEncoding);
        var hash = await provider.HashAsync("SM3", data, ct);
        return new Sm3HashResponse(Encode(hash, req.OutputEncoding), req.OutputEncoding);
    }

    // ────────────────── HMAC-SM3 ──────────────────

    public async Task<HmacGenerateResponse> HmacGenerateAsync(HmacGenerateRequest req, CancellationToken ct)
    {
        var (key, version) = await ResolveKeyAsync(req.KeyId, req.KeyVersion, "MAC", ct);
        var provider = _router.ResolveProvider(version);
        var data = Decode(req.Data, req.Encoding);
        var hmac = await provider.HmacAsync(version.ProviderKeyRef, data, ct);
        await _audit.LogAsync("HMAC_GENERATE", req.KeyId, version.VersionNo, "SUCCESS", ct);
        return new HmacGenerateResponse(req.KeyId, version.VersionNo, Encode(hmac, "BASE64"), "BASE64");
    }

    public async Task<HmacVerifyResponse> HmacVerifyAsync(HmacVerifyRequest req, CancellationToken ct)
    {
        var (key, version) = await ResolveKeyAsync(req.KeyId, req.KeyVersion, "MAC", ct);
        var provider = _router.ResolveProvider(version);
        var data = Decode(req.Data, req.Encoding);
        var expected = await provider.HmacAsync(version.ProviderKeyRef, data, ct);
        var actual = Decode(req.Hmac, req.Encoding);
        var valid = CryptographicOperations.FixedTimeEquals(expected, actual);
        await _audit.LogAsync("HMAC_VERIFY", req.KeyId, version.VersionNo, valid ? "SUCCESS" : "VERIFY_FAILED", ct);
        return new HmacVerifyResponse(req.KeyId, version.VersionNo, valid);
    }

    // ────────────────── Random ──────────────────

    public async Task<RandomResponse> GenerateRandomAsync(RandomRequest req, CancellationToken ct)
    {
        var provider = _router.GetDefaultProvider();
        var result = await provider.GenerateRandomAsync(req.Length, ct);
        return new RandomResponse(Encode(result.Bytes, req.Encoding), req.Encoding);
    }

    // ────────────────── 核心辅助 ──────────────────

    private async Task<(SysKey Key, SysKeyVersion Version)> ResolveKeyAsync(
        string keyId, int? versionNo, string requiredUsage, CancellationToken ct)
    {
        var key = await _db.Keys.FirstOrDefaultAsync(k => k.KeyId == keyId, ct)
            ?? throw new BusinessException("KEY_NOT_FOUND", "密钥不存在");

        // 授权校验
        await _authChecker.CheckAsync(keyId, CurrentContext.AppId, requiredUsage, ct);

        var vNo = versionNo ?? key.CurrentVersion
            ?? throw new BusinessException("KEY_NO_VERSION", "密钥无可用版本");

        var version = await _db.KeyVersions
            .FirstOrDefaultAsync(v => v.KeyId == keyId && v.VersionNo == vNo, ct)
            ?? throw new BusinessException("KEY_VERSION_NOT_FOUND", "版本不存在");

        // 状态校验
        var allowed = version.Status switch
        {
            "ACTIVE" => true,
            "ROTATED" => requiredUsage is "DECRYPT" or "VERIFY", // 历史版本仅允许解密/验签
            _ => false
        };
        if (!allowed)
            throw new BusinessException("KEY_VERSION_NOT_USABLE", $"版本状态 {version.Status} 不允许当前操作");

        return (key, version);
    }

    private static byte[] Decode(string data, string encoding) => encoding.ToUpper() switch
    {
        "BASE64" => Convert.FromBase64String(data),
        "HEX" => Convert.FromHexString(data),
        _ => throw new BusinessException("INVALID_ENCODING", $"不支持的编码: {encoding}")
    };

    private static string Encode(byte[] data, string encoding) => encoding.ToUpper() switch
    {
        "BASE64" => Convert.ToBase64String(data),
        "HEX" => Convert.ToHexString(data).ToLower(),
        _ => throw new BusinessException("INVALID_ENCODING", $"不支持的编码: {encoding}")
    };
}
```

## 35.4 DI 注册

```csharp
public static class CryptoServiceCollectionExtensions
{
    public static IServiceCollection AddCryptoService(this IServiceCollection services)
    {
        services.AddScoped<ICryptoService, CryptoService>();
        return services;
    }
}
```
