namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 应用凭据（仅存哈希，不存明文）
/// </summary>
public sealed class SysApplicationSecret
{
    public long Id { get; set; }
    public long ApplicationId { get; set; }
    public string SecretHash { get; set; } = null!;
    public int SecretVersion { get; set; }
    public byte Status { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
