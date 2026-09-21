namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 管理用户
/// </summary>
public sealed class SysUser
{
    public long Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? DisplayName { get; set; }
    public byte Status { get; set; }
    public int LoginFailCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
}
