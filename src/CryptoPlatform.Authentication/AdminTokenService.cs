using CryptoPlatform.Authentication.PasswordHashing;
using CryptoPlatform.Domain;
using CryptoPlatform.Infrastructure.Caching;
using CryptoPlatform.Persistence;
using CryptoPlatform.Security;
using Microsoft.EntityFrameworkCore;
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
/// 支持：
/// - 可配置 KDF 密码校验（通过 IPasswordHasherFactory）
/// - 密码哈希自动升级策略
/// - 登录失败锁定机制
/// - 安全事件上报
/// - 首次登录强制修改密码标记
/// </summary>
public sealed class AdminTokenService : IAdminTokenService
{
    /// <summary>最大登录失败次数，超过则锁定账号</summary>
    private const int MaxLoginFailCount = 5;

    /// <summary>账号锁定时长（分钟）</summary>
    private const int LockoutMinutes = 30;

    private readonly CryptoPlatformDbContext _db;
    private readonly ITokenGenerator _tokenGen;
    private readonly ICacheService _cache;
    private readonly JwtSettings _jwtSettings;
    private readonly IPasswordHasherFactory _hasherFactory;
    private readonly ISecurityEventService _securityEvents;
    private readonly ILogger<AdminTokenService> _logger;

    public AdminTokenService(
        CryptoPlatformDbContext db,
        ITokenGenerator tokenGen,
        ICacheService cache,
        JwtSettings jwtSettings,
        IPasswordHasherFactory hasherFactory,
        ISecurityEventService securityEvents,
        ILogger<AdminTokenService> logger)
    {
        _db = db;
        _tokenGen = tokenGen;
        _cache = cache;
        _jwtSettings = jwtSettings;
        _hasherFactory = hasherFactory;
        _securityEvents = securityEvents;
        _logger = logger;
    }

    public async Task<AdminLoginResponse> LoginAsync(string username, string password, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username, ct)
            ?? throw new BusinessException("AUTH_LOGIN_FAILED", "用户名或密码错误");

        if (user.Status != 0)
            throw new BusinessException("AUTH_ACCOUNT_DISABLED", "账号已禁用");

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            throw new BusinessException("AUTH_ACCOUNT_LOCKED", "账号已锁定");

        // ── 密码校验：使用存储的算法标识获取对应 Hasher ──
        var hasher = _hasherFactory.GetByAlgorithm(user.PasswordAlgorithm, user.PasswordVersion);
        if (hasher is null)
        {
            _logger.LogError("用户 {Username} 的密码算法 {Algorithm} v{Version} 无法识别",
                username, user.PasswordAlgorithm, user.PasswordVersion);
            throw new BusinessException("AUTH_LOGIN_FAILED", "用户名或密码错误");
        }

        if (!hasher.VerifyPassword(password, user.PasswordHash))
        {
            // ── 登录失败处理 ──
            user.LoginFailCount++;
            if (user.LoginFailCount >= MaxLoginFailCount)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                _logger.LogWarning("用户 {Username} 登录失败 {Count} 次，账号已锁定至 {Until}",
                    username, user.LoginFailCount, user.LockedUntil);

                await _securityEvents.RaiseAsync(
                    "ADMIN_ACCOUNT_LOCKED",
                    user.Id.ToString(),
                    null,
                    $"管理员账号 {username} 因连续登录失败 {user.LoginFailCount} 次被锁定",
                    ct);
            }

            await _db.SaveChangesAsync(ct);

            await _securityEvents.RaiseAsync(
                "ADMIN_LOGIN_FAILED",
                user.Id.ToString(),
                null,
                $"管理员 {username} 登录失败（密码错误），累计失败 {user.LoginFailCount} 次",
                ct);

            throw new BusinessException("AUTH_LOGIN_FAILED", "用户名或密码错误");
        }

        // ── 登录成功：重置失败计数 ──
        user.LoginFailCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

        // ── 密码哈希升级策略 ──
        var defaultHasher = _hasherFactory.GetDefault();
        if (hasher.NeedsUpgrade(user.PasswordHash))
        {
            user.PasswordHash = defaultHasher.HashPassword(password);
            user.PasswordAlgorithm = defaultHasher.Algorithm;
            user.PasswordVersion = defaultHasher.Version;
            user.PasswordChangedAt = DateTime.UtcNow;

            _logger.LogInformation("用户 {Username} 密码哈希已从 {OldAlgorithm} 升级至 {NewAlgorithm} v{Version}",
                username, hasher.Algorithm, defaultHasher.Algorithm, defaultHasher.Version);
        }

        await _db.SaveChangesAsync(ct);

        // ── 签发 Token ──
        var role = user.UserRoles.FirstOrDefault()?.Role?.RoleCode ?? "SYSTEM_ADMIN";
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

        _logger.LogInformation("管理员 {Username} 登录成功，角色 {Role}", username, role);

        await _securityEvents.RaiseAsync(
            "ADMIN_LOGIN_SUCCESS",
            user.Id.ToString(),
            null,
            $"管理员 {username} 登录成功",
            ct);

        return new AdminLoginResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: _jwtSettings.ExpiresInSeconds,
            OperatorId: user.Id.ToString(),
            Role: role,
            MustModifyPassword: user.MustModifyPassword);
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
            TimeSpan.FromHours(2), ct);
        _logger.LogInformation("令牌 {Jti} 已撤销", jti);
    }

    /// <summary>
    /// 保留旧 SM3 哈希工具方法，仅用于向后兼容测试或迁移场景。
    /// 新代码不应使用此方法。
    /// </summary>
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
