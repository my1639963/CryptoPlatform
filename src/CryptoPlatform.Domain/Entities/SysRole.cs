namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 角色定义
/// </summary>
public sealed class SysRole
{
    public long Id { get; set; }
    public string RoleCode { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
