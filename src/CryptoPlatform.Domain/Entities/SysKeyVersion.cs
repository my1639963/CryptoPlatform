namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 密钥版本。表示某一把密钥的具体密码学材料。
/// HSM 场景下 ProviderKeyRef 指向 HSM 内部句柄；
/// 软件密码模块场景下 EncryptedKeyMaterial 存储 KEK 加密后的密钥材料。
/// </summary>
public sealed class SysKeyVersion
{
    public long Id { get; set; }
    public string KeyId { get; set; } = null!;
    public int VersionNo { get; set; }
    public string ProviderType { get; set; } = null!;
    public string? DeviceId { get; set; }
    public string ProviderKeyRef { get; set; } = null!;
    public string? PublicKeyMaterial { get; set; }
    public string? EncryptedKeyMaterial { get; set; }
    public string Fingerprint { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? RotatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? DestroyedAt { get; set; }
    public string? DestroyResult { get; set; }
    public long UsageCount { get; set; }
    public long UsageBytes { get; set; }
    public string? CreatedRequestId { get; set; }
    public string ConcurrencyStamp { get; set; } = null!;
}
