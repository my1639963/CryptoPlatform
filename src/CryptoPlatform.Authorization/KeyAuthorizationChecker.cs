using CryptoPlatform.Domain;
using CryptoPlatform.Domain.Entities;
using CryptoPlatform.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Authorization;

/// <summary>
/// 密钥授权检查器。
/// 校验规则：
/// 1. Owner 应用直接放行；
/// 2. 非 Owner 检查显式授权记录（sys_key_authorization）；
/// 3. 授权记录必须为 ACTIVE 状态且未过期；
/// 4. 授权权限列表必须包含所需操作。
/// </summary>
public interface IKeyAuthorizationChecker
{
    /// <summary>
    /// 检查指定应用是否有权对指定密钥执行指定操作。
    /// </summary>
    /// <param name="keyId">密钥 ID</param>
    /// <param name="callerAppId">调用方应用 ID</param>
    /// <param name="requiredPermission">所需权限（ENCRYPT / DECRYPT / SIGN / VERIFY / MAC）</param>
    /// <param name="ct">取消令牌</param>
    /// <exception cref="BusinessException">无权限时抛出</exception>
    Task CheckAsync(string keyId, string callerAppId, string requiredPermission, CancellationToken ct);
}

public sealed class KeyAuthorizationChecker : IKeyAuthorizationChecker
{
    private readonly CryptoPlatformDbContext _db;
    private readonly ILogger<KeyAuthorizationChecker> _logger;

    public KeyAuthorizationChecker(
        CryptoPlatformDbContext db,
        ILogger<KeyAuthorizationChecker> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CheckAsync(string keyId, string callerAppId, string requiredPermission, CancellationToken ct)
    {
        var key = await _db.Keys.FirstOrDefaultAsync(k => k.KeyId == keyId, ct);
        if (key is null)
            throw new BusinessException("KEY_NOT_FOUND", "密钥不存在");

        // Owner 直接放行
        if (key.OwnerAppId == callerAppId)
            return;

        // 检查显式授权
        var auth = await _db.KeyAuthorizations
            .FirstOrDefaultAsync(a =>
                a.KeyId == keyId &&
                a.AppId == callerAppId &&
                a.Status == "ACTIVE", ct);

        if (auth is null)
        {
            _logger.LogWarning("密钥访问被拒绝：应用 {AppId} 无权访问密钥 {KeyId}", callerAppId, keyId);
            throw new BusinessException("KEY_PERMISSION_DENIED", "无权访问此密钥");
        }

        // 过期校验
        if (auth.ExpiresAt.HasValue && auth.ExpiresAt < DateTime.UtcNow)
            throw new BusinessException("KEY_AUTH_EXPIRED", "授权已过期");

        // 权限校验
        var permissions = auth.Permissions.Split(',', StringSplitOptions.TrimEntries);
        if (!permissions.Contains(requiredPermission, StringComparer.OrdinalIgnoreCase))
            throw new BusinessException("KEY_PERMISSION_DENIED", $"无 {requiredPermission} 权限");
    }
}
