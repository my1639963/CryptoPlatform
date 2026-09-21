namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 权限定义
/// </summary>
public sealed class SysPermission
{
    public long Id { get; set; }
    public string PermissionCode { get; set; } = null!;
    public string PermissionName { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
