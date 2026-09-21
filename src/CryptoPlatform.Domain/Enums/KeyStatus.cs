namespace CryptoPlatform.Domain.Enums;

/// <summary>
/// 密钥生命周期状态
/// </summary>
public enum KeyStatus
{
    CREATED,
    ACTIVE,
    ROTATED,
    DISABLED,
    EXPIRED,
    REVOKED,
    DESTROYED
}
