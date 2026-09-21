using System.Text;
using CryptoPlatform.Domain;
using CryptoPlatform.Domain.Entities;
using CryptoPlatform.Infrastructure.Caching;
using CryptoPlatform.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace CryptoPlatform.Authentication;

/// <summary>
/// 应用凭据认证服务接口。
/// 基于 HMAC-SM3 签名 + 防重放（Nonce + Timestamp）。
/// </summary>
public interface IAppAuthenticationService
{
    /// <summary>
    /// 验证应用请求签名。
    /// </summary>
    /// <param name="appId">应用 ID</param>
    /// <param name="timestamp">请求时间戳（秒）</param>
    /// <param name="nonce">随机数</param>
    /// <param name="signature">请求方计算的 HMAC-SM3 签名</param>
    /// <param name="httpMethod">HTTP 方法</param>
    /// <param name="requestPath">请求路径</param>
    /// <param name="body">请求体（可为 null）</param>
    /// <param name="ct">取消令牌</param>
    Task<AppAuthResult> AuthenticateAsync(
        string appId, string timestamp, string nonce, string signature,
        string httpMethod, string requestPath, string? body, CancellationToken ct);
}

/// <summary>
/// 应用凭据认证服务实现。
/// 签名算法：HMAC-SM3(AppSecret, AppId + Timestamp + Nonce + Method + Path + BodyHash)
/// </summary>
public sealed class AppAuthenticationService : IAppAuthenticationService
{
    private readonly CryptoPlatformDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<AppAuthenticationService> _logger;

    /// <summary>时间戳允许的最大偏差（秒）</summary>
    private const int MaxTimestampDrift = 300; // 5 分钟

    public AppAuthenticationService(
        CryptoPlatformDbContext db,
        ICacheService cache,
        ILogger<AppAuthenticationService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AppAuthResult> AuthenticateAsync(
        string appId, string timestamp, string nonce, string signature,
        string httpMethod, string requestPath, string? body, CancellationToken ct)
    {
        // 1. 验证时间戳（防重放：5 分钟内）
        if (!long.TryParse(timestamp, out var ts))
            return new AppAuthResult(false, "AUTH_INVALID_TIMESTAMP", "无效的时间戳");

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;
        var drift = Math.Abs((DateTime.UtcNow - requestTime).TotalSeconds);
        if (drift > MaxTimestampDrift)
            return new AppAuthResult(false, "AUTH_TIMESTAMP_EXPIRED", "时间戳已过期或偏差过大");

        // 2. 验证 Nonce 唯一性（防重放）
        var nonceKey = $"nonce:{appId}:{nonce}";
        var existingNonce = await _cache.GetStringAsync(nonceKey);
        if (existingNonce is not null)
            return new AppAuthResult(false, "NONCE_REPLAYED", "随机数已被使用，疑似重放攻击");

        // 3. 查找应用
        var app = await _db.Applications
            .FirstOrDefaultAsync(a => a.AppId == appId && a.Status == 1, ct);
        if (app is null)
            return new AppAuthResult(false, "AUTH_APP_NOT_FOUND", "应用不存在或已禁用");

        // 4. 查找有效凭据（未撤销、未过期）
        var secrets = await _db.ApplicationSecrets
            .Where(s => s.ApplicationId == app.Id
                     && s.Status == 1
                     && s.RevokedAt == null
                     && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(s => s.SecretVersion)
            .ToListAsync(ct);

        if (secrets.Count == 0)
            return new AppAuthResult(false, "AUTH_NO_VALID_SECRET", "无有效凭据");

        // 5. 逐个尝试验证签名（支持多版本凭据）
        var bodyHash = body is null ? "" : ComputeSM3Hash(body);
        var stringToSign = $"{appId}|{timestamp}|{nonce}|{httpMethod.ToUpperInvariant()}|{requestPath}|{bodyHash}";

        bool signatureValid = false;
        SysApplicationSecret? matchedSecret = null;

        foreach (var secret in secrets)
        {
            var computed = ComputeHmacSm3(secret.SecretHash, stringToSign);
            if (string.Equals(computed, signature, StringComparison.OrdinalIgnoreCase))
            {
                signatureValid = true;
                matchedSecret = secret;
                break;
            }
        }

        if (!signatureValid)
        {
            _logger.LogWarning("应用 {AppId} 签名验证失败", appId);
            return new AppAuthResult(false, "AUTH_SIGNATURE_INVALID", "签名验证失败");
        }

        // 6. 签名通过 → 更新 Nonce 缓存 + 更新凭据最后使用时间
        await _cache.SetStringAsync(nonceKey, "1",
            TimeSpan.FromSeconds(MaxTimestampDrift * 2), ct);

        matchedSecret!.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogDebug("应用 {AppId} 认证成功", appId);
        return new AppAuthResult(true);
    }

    /// <summary>
    /// 计算 SM3 哈希（用于请求体摘要）。
    /// </summary>
    private static string ComputeSM3Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var digest = new SM3Digest();
        digest.BlockUpdate(bytes, 0, bytes.Length);
        var hash = new byte[digest.GetDigestSize()];
        digest.DoFinal(hash, 0);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// 计算 HMAC-SM3 签名。
    /// 注意：此处使用 SecretHash 作为 HMAC 密钥（而非明文 Secret）。
    /// 因为服务端不保存明文 Secret，只能使用哈希值作为密钥。
    /// </summary>
    private static string ComputeHmacSm3(string secretKey, string message)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var msgBytes = Encoding.UTF8.GetBytes(message);

        var hmac = new HMac(new SM3Digest());
        hmac.Init(new KeyParameter(keyBytes));
        hmac.BlockUpdate(msgBytes, 0, msgBytes.Length);

        var result = new byte[hmac.GetMacSize()];
        hmac.DoFinal(result, 0);
        return Convert.ToHexString(result);
    }
}
