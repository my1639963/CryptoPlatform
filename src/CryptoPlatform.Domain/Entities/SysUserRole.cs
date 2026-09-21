namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 用户-角色关联（多对多）
/// </summary>
public sealed class SysUserRole
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>角色导航属性</summary>
    public SysRole? Role { get; set; }
}
