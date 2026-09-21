namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 逻辑密钥。KeyId 在整个生命周期内保持不变。
/// KeyType、KeyUsage、OwnerAppId 创建后不可修改。
/// </summary>
public sealed class SysKey
{
    public long Id { get; set; }
    public string KeyId { get; set; } = null!;
    public string OwnerAppId { get; set; } = null!;
    public string KeyType { get; set; } = null!;
    public string KeyUsage { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int? CurrentVersion { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DestroyedAt { get; set; }
    public string? Description { get; set; }
    public string ConcurrencyStamp { get; set; } = null!;

    public ICollection<SysKeyVersion> Versions { get; set; } = new List<SysKeyVersion>();
    public ICollection<SysKeyAuthorization> Authorizations { get; set; } = new List<SysKeyAuthorization>();
}
