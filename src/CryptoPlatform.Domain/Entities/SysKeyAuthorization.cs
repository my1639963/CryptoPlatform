namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 密钥授权。记录某个应用对某把密钥的允许操作集合。
/// </summary>
public sealed class SysKeyAuthorization
{
    public long Id { get; set; }
    public string KeyId { get; set; } = null!;
    public string AppId { get; set; } = null!;
    public string Permissions { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
}
