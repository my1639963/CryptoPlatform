namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 管理用户
/// </summary>
public sealed class SysUser
{
    public long Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    /// <summary>密码哈希算法标识，例如 PBKDF2-SHA256</summary>
    public string PasswordAlgorithm { get; set; } = "PBKDF2-SHA256";
    /// <summary>密码算法版本，用于升级判断</summary>
    public int PasswordVersion { get; set; } = 1;
    /// <summary>密码最后修改时间</summary>
    public DateTime? PasswordChangedAt { get; set; }
    /// <summary>首次登录或管理员强制修改标记</summary>
    public bool MustModifyPassword { get; set; }
    public string? DisplayName { get; set; }
    public byte Status { get; set; }
    public int LoginFailCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
}
