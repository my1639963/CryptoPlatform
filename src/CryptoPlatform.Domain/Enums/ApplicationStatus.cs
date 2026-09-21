namespace CryptoPlatform.Domain.Enums;

/// <summary>
/// 接入应用状态
/// </summary>
public enum ApplicationStatus : byte
{
    /// <summary>正常</summary>
    Active = 0,

    /// <summary>禁用</summary>
    Disabled = 1,

    /// <summary>锁定（安全事件触发）</summary>
    Locked = 2
}
