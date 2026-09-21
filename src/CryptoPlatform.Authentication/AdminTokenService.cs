using CryptoPlatform.Domain;
using CryptoPlatform.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Authentication;

/// <summary>
/// 管理员 Token 服务接口。
/// </summary>
public interface IAdminTokenService
{
    Task<AdminLoginResponse> LoginAsync(string username, string password, CancellationToken ct);
    Task<AdminPrincipal?> ValidateAsync(string token);
    Task RevokeAsync(string jti, CancellationToken ct);
}

/// <summary>
/// 管理员 Token 服务实现。
/// 支持登录（密码校验 + 锁定机制）、令牌验证、令牌撤销（基于缓存）。
/// </summary>
public sealed class AdminTokenService : IAdminTokenService
{
    private readonly CryptoPlatformDbContext _db;
    private readonly ITokenGenerator _tokenGen;
    private readonly IDistributedCache _cache;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AdminTokenService> _logger;

    public AdminTokenService(
        CryptoPlatformDbContext db,
        ITokenGenerator tokenGen,
        IDistributedCache cache,
        JwtSettings jwtSettings,
        ILogger<AdminTokenService> logger)
    {
        _db = db;
        _tokenGen = tokenGen;
        _cache = cache;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    public async Task<AdminLoginResponse> LoginAsync(string username, string password, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Username == username, ct)
            ?? throw new BusinessException("AUTH_LOGIN_FAILED", "用户名或密码错误");

        if (user.Status != 0)
            throw new BusinessException("AUTH_ACCOUNT_DISABLED", "账号已禁用");

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            throw new BusinessException("AUTH_ACCOUNT_LOCKED", "账号已锁定");

        if (!VerifyPassword(password, user.PasswordHash))
        {
            user.LoginFailCount++;
            if (user.LoginFailCount >= 5)
                user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
            await _db.SaveChangesAsync(ct);
            throw new BusinessException("AUTH_LOGIN_FAILED", "用户名或密码错误");
        }

        // 登录成功
        user.LoginFailCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var role = "SYSTEM_ADMIN";
        var scopes = GetScopesForRole(role);
        var jti = Guid.NewGuid().ToString("N");

        var token = _tokenGen.Generate(new TokenPayload(
            Jti: jti,
            Issuer: _jwtSettings.Issuer,
            Subject: user.Id.ToString(),
            Audience: _jwtSettings.Audience,
            OperatorId: user.Id.ToString(),
            Role: role,
            Scopes: scopes,
            Expires: DateTime.UtcNow.AddSeconds(_jwtSettings.ExpiresInSeconds)));

        _logger.LogInformation("管理员 {Username} 登录成功", username);

        return new AdminLoginResponse(token, "Bearer", _jwtSettings.ExpiresInSeconds, user.Id.ToString(), role);
    }

    public async Task<AdminPrincipal?> ValidateAsync(string token)
    {
        var payload = _tokenGen.Validate(token);
        if (payload is null) return null;

        // 检查是否已撤销
        var revoked = await _cache.GetStringAsync($"token:revoked:{payload.Jti}");
        if (revoked is not null) return null;

        return new AdminPrincipal(payload.OperatorId, "", payload.Role, payload.Scopes);
    }

    public async Task RevokeAsync(string jti, CancellationToken ct)
    {
        await _cache.SetStringAsync($"token:revoked:{jti}", "1",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
            });
        _logger.LogInformation("令牌 {Jti} 已撤销", jti);
    }

    /// <summary>
    /// 密码校验。
    /// 简单实现：对比 SM3 哈希。生产环境应使用更安全的 KDF。
    /// </summary>
    private static bool VerifyPassword(string password, string storedHash)
    {
        // 简化实现：直接比较哈希值
        var hash = ComputeSM3Hash(password);
        return string.Equals(hash, storedHash, StringComparison.Ordinal);
    }

    public static string ComputeSM3Hash(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var digest = new Org.BouncyCastle.Crypto.Digests.SM3Digest();
        digest.BlockUpdate(bytes, 0, bytes.Length);
        var hash = new byte[digest.GetDigestSize()];
        digest.DoFinal(hash, 0);
        return Convert.ToHexString(hash);
    }

    private static IReadOnlyList<string> GetScopesForRole(string role) => role switch
    {
        "SYSTEM_ADMIN" => new[] { "key:manage", "app:manage", "user:manage", "system:manage", "audit:read" },
        "KEY_ADMIN" => new[] { "key:manage", "key:read", "audit:read" },
        "APP_ADMIN" => new[] { "app:manage", "app:read" },
        "SECURITY_AUDITOR" => new[] { "audit:read", "security:read" },
        _ => Array.Empty<string>()
    };
}
