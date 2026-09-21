namespace CryptoPlatform.Domain.Enums;

/// <summary>
/// 安全事件处置状态
/// </summary>
public enum SecurityEventStatus
{
    OPEN,
    ACKNOWLEDGED,
    HANDLING,
    RESOLVED,
    CLOSED
}
